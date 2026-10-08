package com.campusrift.tech

import android.app.Activity
import android.Manifest
import android.content.pm.PackageManager
import android.media.AudioFormat
import android.media.AudioRecord
import android.media.MediaRecorder
import android.os.SystemClock
import org.vosk.Model
import org.vosk.Recognizer
import org.json.JSONObject
import java.io.File
import java.text.Normalizer
import java.util.concurrent.Executors
import java.util.concurrent.atomic.AtomicInteger

/** PCM lives only in a 100ms buffer. No audio/text files, upload or transcription logs. */
class VoiceBridge(activity: Activity, private val listener: Listener) {
    interface Listener { fun onKeyword(generation: Int, keyword: Int); fun onState(state: String) }
    private val context=activity.applicationContext
    private val worker=Executors.newSingleThreadExecutor()
    private val token=AtomicInteger(0)
    @Volatile private var closed=false
    @Volatile private var recorder: AudioRecord?=null
    private var model: Model?=null
    private val names=arrayOf("vạn kiếm quy tông","băng thiên lôi ngục","thiên thủ hấp tinh")
    private fun normalize(s: String)=Normalizer.normalize(s.lowercase(),Normalizer.Form.NFD).replace(Regex("\\p{M}"),"").replace('đ','d').trim()
    private fun copyAssets(path: String, destination: File) {
        val children=context.assets.list(path) ?: emptyArray()
        if(children.isEmpty()){destination.parentFile?.mkdirs();context.assets.open(path).use { input -> destination.outputStream().use { input.copyTo(it) } }}
        else {destination.mkdirs();children.forEach { copyAssets("$path/$it",File(destination,it)) }}
    }
    private fun loadModel(){if(model!=null||closed)return;listener.onState("loading");val folder=File(context.filesDir,"vosk-small-vn-0.4");val marker=File(folder,"ready-v1");if(!marker.exists()){copyAssets("vosk",folder);marker.writeText("0.4")};if(!closed)model=Model(folder.absolutePath)}
    fun prepare(){worker.execute {try{loadModel();if(!closed)listener.onState("ready")}catch(_: Exception){if(!closed)listener.onState("voice unavailable")}}}
    fun start(generation: Int) {
        val request=token.incrementAndGet()
        worker.execute {
            if(closed||request!=token.get())return@execute
            var recognition: Recognizer?=null;var audio: AudioRecord?=null
            try {
                if(context.checkSelfPermission(Manifest.permission.RECORD_AUDIO)!=PackageManager.PERMISSION_GRANTED)throw SecurityException("Microphone denied")
                loadModel();if(closed||request!=token.get()||model==null)return@execute
                recognition=Recognizer(model,16000f,"[\"vạn kiếm quy tông\",\"băng thiên lôi ngục\",\"thiên thủ hấp tinh\",\"[unk]\"]")
                val size=maxOf(6400,AudioRecord.getMinBufferSize(16000,AudioFormat.CHANNEL_IN_MONO,AudioFormat.ENCODING_PCM_16BIT))
                audio=AudioRecord(MediaRecorder.AudioSource.VOICE_RECOGNITION,16000,AudioFormat.CHANNEL_IN_MONO,AudioFormat.ENCODING_PCM_16BIT,size)
                if(audio.state!=AudioRecord.STATE_INITIALIZED)throw IllegalStateException("Microphone unavailable")
                recorder=audio;audio.startRecording();listener.onState("listening")
                val samples=ShortArray(1600);val until=SystemClock.elapsedRealtime()+4000;var emitted=-1
                while(!closed&&request==token.get()&&SystemClock.elapsedRealtime()<until) {
                    val count=audio.read(samples,0,samples.size);if(count<=0)break
                    val full=recognition.acceptWaveForm(samples,count)
                    val text=JSONObject(if(full)recognition.result else recognition.partialResult).optString(if(full)"text" else "partial")
                    val id=names.indexOfFirst { normalize(it)==normalize(text) }
                    if(id>=0&&id!=emitted){emitted=id;listener.onKeyword(generation,id)}
                    samples.fill(0)
                }
            } catch(error: Exception){if(!closed&&request==token.get())listener.onState("voice unavailable")}
            finally {try{audio?.stop()}catch(_: Exception){};audio?.release();recorder=null;recognition?.close()}
        }
    }
    fun stop(){token.incrementAndGet();try{recorder?.stop()}catch(_: Exception){}}
    fun close(){closed=true;stop();worker.execute {model?.close();model=null};worker.shutdown()}
}
