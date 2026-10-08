package com.campusrift.gesture

import android.app.Activity
import android.os.Handler
import android.os.HandlerThread
import android.os.SystemClock
import com.google.mediapipe.framework.image.ByteBufferImageBuilder
import com.google.mediapipe.framework.image.MPImage
import com.google.mediapipe.tasks.core.BaseOptions
import com.google.mediapipe.tasks.core.Delegate
import com.google.mediapipe.tasks.components.processors.ClassifierOptions
import com.google.mediapipe.tasks.vision.core.ImageProcessingOptions
import com.google.mediapipe.tasks.vision.core.RunningMode
import com.google.mediapipe.tasks.vision.gesturerecognizer.GestureRecognizer
import java.nio.ByteBuffer
import java.util.concurrent.atomic.AtomicBoolean

/** One owned flight. Close completes on the worker before its image is reclaimed. */
class GestureBridge(activity: Activity, private val listener: GestureListener, preferred: String) {
    interface GestureListener {
        fun onResult(epoch: Int, frameId: Long, label: String, score: Float, handed: String,
                     landmarks: FloatArray, world: FloatArray, labels: Array<String>, scores: FloatArray,
                     full: Boolean, hand: Boolean, tsMs: Long, w: Int, h: Int, startNs: Long, resultNs: Long)
        fun onHands(epoch: Int,frameId: Long,labels: Array<String>,scores: FloatArray,handed: Array<String>,landmarks: FloatArray,world: FloatArray,ts: Long,w: Int,h: Int,startNs: Long,resultNs: Long)
        fun onState(epoch: Int, delegateName: String, ready: Boolean, error: String)
    }
    private val context = activity.applicationContext
    private val worker = HandlerThread("CampusRiftGestures").apply { start() }
    private val handler = Handler(worker.looper)
    private val busy = AtomicBoolean(false)
    private val closed = AtomicBoolean(false)
    private val ready = AtomicBoolean(false)
    private var recognizer: GestureRecognizer? = null
    private var owned: ByteBuffer? = null
    private var image: MPImage? = null
    private var gpu = preferred != "CPU"
    private var handCount=1
    private var generation = 0
    private var stateEpoch = 0
    private data class Flight(val epoch: Int, val id: Long, val ts: Long, val w: Int, val h: Int, var start: Long = 0)
    private var flight: Flight? = null
    init { handler.post { create(gpu, 0) } }
    private fun release() { image?.close(); image=null; flight=null; busy.set(false) }
    private fun state(error: String = "") {
        if (!closed.get()) listener.onState(stateEpoch, if(gpu) "GPU" else "CPU", ready.get(), error)
    }
    private fun create(useGpu: Boolean, epoch: Int) {
        ready.set(false); stateEpoch=epoch; generation++
        val token=generation
        try {
            recognizer?.close(); recognizer=null; release(); gpu=useGpu
            val options=GestureRecognizer.GestureRecognizerOptions.builder()
                .setBaseOptions(BaseOptions.builder().setModelAssetPath("gesture_recognizer.task")
                    .setDelegate(if(gpu) Delegate.GPU else Delegate.CPU).build())
                .setRunningMode(RunningMode.LIVE_STREAM).setNumHands(handCount)
                .setMinHandDetectionConfidence(.4f).setMinHandPresenceConfidence(.4f).setMinTrackingConfidence(.4f)
                .setCannedGesturesClassifierOptions(ClassifierOptions.builder().setMaxResults(8).setScoreThreshold(0f).build())
                .setResultListener { result, _ ->
                    val done=SystemClock.elapsedRealtimeNanos()
                    handler.post {
                        if(token!=generation || closed.get()) return@post
                        val f=flight ?: return@post
                        try {
                            if(handCount==2){
                                val count=minOf(2,result.landmarks().size,result.worldLandmarks().size)
                                val labels=Array(count){"None"};val scores=FloatArray(count);val handed=Array(count){""};val norm=FloatArray(count*63);val xyz=FloatArray(count*63)
                                for(i in 0 until count){val category=result.gestures().getOrNull(i)?.maxByOrNull{it.score()};labels[i]=category?.categoryName()?:"None";scores[i]=category?.score()?:0f;handed[i]=result.handedness().getOrNull(i)?.firstOrNull()?.categoryName()?:"";result.landmarks()[i].forEachIndexed{j,p->norm[i*63+j*3]=p.x();norm[i*63+j*3+1]=p.y();norm[i*63+j*3+2]=p.z()};result.worldLandmarks()[i].forEachIndexed{j,p->xyz[i*63+j*3]=p.x();xyz[i*63+j*3+1]=p.y();xyz[i*63+j*3+2]=p.z()}}
                                listener.onHands(f.epoch,f.id,labels,scores,handed,norm,xyz,f.ts,f.w,f.h,f.start,done)
                            }else{
                            val categories=result.gestures().firstOrNull().orEmpty().sortedByDescending { it.score() }
                            val points=result.landmarks().firstOrNull();val world=result.worldLandmarks().firstOrNull()
                            val norm=if(points?.size==21) FloatArray(63) else FloatArray(0)
                            val xyz=if(world?.size==21) FloatArray(63) else FloatArray(0)
                            if(norm.isNotEmpty()) points!!.forEachIndexed { i,p -> norm[i*3]=p.x();norm[i*3+1]=p.y();norm[i*3+2]=p.z() }
                            if(xyz.isNotEmpty()) world!!.forEachIndexed { i,p -> xyz[i*3]=p.x();xyz[i*3+1]=p.y();xyz[i*3+2]=p.z() }
                            val labels=categories.map { it.categoryName() }.toTypedArray();val scores=categories.map { it.score() }.toFloatArray()
                            listener.onResult(f.epoch,f.id,labels.firstOrNull() ?: "None",scores.firstOrNull() ?: 0f,
                                result.handedness().firstOrNull()?.firstOrNull()?.categoryName() ?: "",
                                norm,xyz,labels,scores,labels.toSet().size==8,norm.isNotEmpty(),f.ts,f.w,f.h,f.start,done)
                            }
                        } finally { release() }
                    }
                }
                .setErrorListener { error -> handler.post {
                    if(token==generation && !closed.get()) { ready.set(false); state(error.message ?: "Inference error") }
                } }.build()
            recognizer=GestureRecognizer.createFromOptions(context,options); ready.set(true);state()
        } catch(error: Exception) {
            ready.set(false)
            if(useGpu && !closed.get()) create(false,epoch)
            else state(error.message ?: "Initialization error")
        }
    }
    @Synchronized fun submit(rgba: ByteBuffer,w: Int,h: Int,rotation: Int,ts: Long,epoch: Int,id: Long): Boolean {
        val count=w*h*4
        if(closed.get() || !ready.get() || w<=0 || h<=0 || rgba.capacity()!=count || !busy.compareAndSet(false,true)) return false
        try {
            if(owned?.capacity()!=count) owned=ByteBuffer.allocateDirect(count)
            val bytes=owned!!;rgba.rewind();bytes.clear();bytes.put(rgba);bytes.rewind()
            handler.post {
                if(closed.get()) { busy.set(false);return@post }
                try {
                    val f=Flight(epoch,id,ts,w,h);flight=f
                    image=ByteBufferImageBuilder(bytes,w,h,MPImage.IMAGE_FORMAT_RGBA).build()
                    f.start=SystemClock.elapsedRealtimeNanos()
                    recognizer!!.recognizeAsync(image,ImageProcessingOptions.builder().setRotationDegrees(rotation).build(),ts)
                } catch(error: Exception) { ready.set(false);state(error.message ?: "Submit error") }
            }
            return true
        } catch(error: Exception) {busy.set(false);return false}
    }
    fun setHandCount(epoch: Int,count: Int){ready.set(false);handler.post {if(!closed.get()){handCount=if(count==2)2 else 1;create(gpu,epoch)}}}
    fun clockNanos(): Long = SystemClock.elapsedRealtimeNanos()
    fun recover(epoch: Int,forceCpu: Boolean) {
        ready.set(false)
        handler.post { if(!closed.get()) create(if(forceCpu) false else gpu,epoch) }
    }
    fun selectDelegate(epoch: Int, preferred: String) {
        ready.set(false)
        handler.post { if(!closed.get()) create(preferred != "CPU",epoch) }
    }
    fun close() {
        if(!closed.compareAndSet(false,true)) return
        ready.set(false)
        handler.post { try { generation++;recognizer?.close();recognizer=null } finally { release();owned=null;worker.quitSafely() } }
    }
}
