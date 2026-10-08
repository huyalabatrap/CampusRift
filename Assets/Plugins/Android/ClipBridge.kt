package com.campusrift.tech

import android.app.*
import android.content.*
import android.content.pm.ServiceInfo
import android.hardware.display.DisplayManager
import android.hardware.display.VirtualDisplay
import android.media.MediaRecorder
import android.media.projection.MediaProjection
import android.media.projection.MediaProjectionManager
import android.net.Uri
import android.os.*
import android.provider.MediaStore

/** Only a player-confirmed request launches system consent. Silent video, no uploads. */
class ClipBridge(private val activity: Activity, private val listener: Listener) {
    interface Listener { fun onState(state: String) }
    companion object { @Volatile var callback: Listener?=null;@Volatile var requested=false }
    fun request(){if(Build.VERSION.SDK_INT<29){listener.onState("unavailable");return};if(requested)return;requested=true;callback=listener;listener.onState("consent");activity.runOnUiThread {try{activity.startActivity(Intent(activity,ClipConsentActivity::class.java))}catch(_: Exception){requested=false;callback?.onState("unavailable")}}}
    fun stop(){val running=ClipRecordingService.active;if(!requested&&!running)return;requested=false;if(running){try{activity.startService(Intent(activity,ClipRecordingService::class.java).setAction("stop"))}catch(_: Exception){activity.stopService(Intent(activity,ClipRecordingService::class.java))}}else callback?.onState("cancelled")}
    fun close(){stop();callback=null}
}
class ClipConsentActivity: Activity() {
    private var launched=false
    override fun onCreate(state: Bundle?){super.onCreate(state);launched=state?.getBoolean("launched")?:false;if(!ClipBridge.requested){finish();return};if(!launched){launched=true;val manager=getSystemService(MEDIA_PROJECTION_SERVICE) as MediaProjectionManager;startActivityForResult(manager.createScreenCaptureIntent(),604)}}
    override fun onSaveInstanceState(state: Bundle){state.putBoolean("launched",launched);super.onSaveInstanceState(state)}
    override fun onActivityResult(request: Int,result: Int,data: Intent?){super.onActivityResult(request,result,data);if(request!=604)return;if(result==RESULT_OK&&data!=null&&ClipBridge.requested&&Build.VERSION.SDK_INT>=29){val intent=Intent(this,ClipRecordingService::class.java).putExtra("result",result).putExtra("data",data);startForegroundService(intent)}else{ClipBridge.requested=false;ClipBridge.callback?.onState("cancelled")};finish()}
}
class ClipRecordingService: Service() {
    companion object { @Volatile var active=false }
    private var projection: MediaProjection?=null;private var display: VirtualDisplay?=null;private var recorder: MediaRecorder?=null
    private var file: ParcelFileDescriptor?=null;private var uri: Uri?=null;private var recording=false;private var stopping=false
    private val handler=Handler(Looper.getMainLooper())
    private val timeout=Runnable {finishRecording()}
    override fun onBind(intent: Intent?) : IBinder?=null
    override fun onStartCommand(intent: Intent?,flags: Int,startId: Int): Int {
        if(intent?.action=="stop"){finishRecording();return START_NOT_STICKY}
        if(Build.VERSION.SDK_INT<29||intent==null||!ClipBridge.requested){stopSelf();return START_NOT_STICKY}
        if(recording)return START_NOT_STICKY
        try {
            val manager=getSystemService(NOTIFICATION_SERVICE) as NotificationManager
            manager.createNotificationChannel(NotificationChannel("campus_clip","Campus Rift clip",NotificationManager.IMPORTANCE_LOW))
            val stop=PendingIntent.getService(this,1,Intent(this,ClipRecordingService::class.java).setAction("stop"),PendingIntent.FLAG_IMMUTABLE or PendingIntent.FLAG_UPDATE_CURRENT)
            val notification=Notification.Builder(this,"campus_clip").setContentTitle("Campus Rift · recording").setContentText("Video includes the real room · tap Stop").setSmallIcon(android.R.drawable.presence_video_online).setOngoing(true).addAction(Notification.Action.Builder(null,"Stop",stop).build()).build()
            startForeground(604,notification,ServiceInfo.FOREGROUND_SERVICE_TYPE_MEDIA_PROJECTION)
            val values=ContentValues().apply {put(MediaStore.Video.Media.DISPLAY_NAME,"CampusRift-${System.currentTimeMillis()}.mp4");put(MediaStore.Video.Media.MIME_TYPE,"video/mp4");put(MediaStore.Video.Media.RELATIVE_PATH,"Movies/CampusRift");put(MediaStore.Video.Media.IS_PENDING,1)}
            uri=contentResolver.insert(MediaStore.Video.Media.EXTERNAL_CONTENT_URI,values)?:throw IllegalStateException("Gallery unavailable")
            file=contentResolver.openFileDescriptor(uri!!,"w")?:throw IllegalStateException("Gallery unavailable")
            val metrics=resources.displayMetrics;val longSide=1280;val shortSide=(longSide*minOf(metrics.widthPixels,metrics.heightPixels)/maxOf(metrics.widthPixels,metrics.heightPixels)/2)*2
            val width=if(metrics.widthPixels>=metrics.heightPixels)longSide else shortSide;val height=if(metrics.widthPixels>=metrics.heightPixels)shortSide else longSide
            val media=MediaRecorder();recorder=media;media.setVideoSource(MediaRecorder.VideoSource.SURFACE);media.setOutputFormat(MediaRecorder.OutputFormat.MPEG_4);media.setVideoEncoder(MediaRecorder.VideoEncoder.H264);media.setVideoSize(width,height);media.setVideoFrameRate(24);media.setVideoEncodingBitRate(3000000);media.setOutputFile(file!!.fileDescriptor);media.prepare()
            val consent=intent.getParcelableExtra<Intent>("data")?:throw IllegalStateException("Consent missing")
            projection=(getSystemService(MEDIA_PROJECTION_SERVICE) as MediaProjectionManager).getMediaProjection(intent.getIntExtra("result",Activity.RESULT_CANCELED),consent)
            projection!!.registerCallback(object:MediaProjection.Callback(){override fun onStop(){finishRecording()}},handler)
            display=projection!!.createVirtualDisplay("Campus Rift clip",width,height,metrics.densityDpi,DisplayManager.VIRTUAL_DISPLAY_FLAG_AUTO_MIRROR,media.surface,null,handler)
            media.start();recording=true;active=true;ClipBridge.callback?.onState("recording");handler.postDelayed(timeout,90000)
        }catch(_: Exception){finishRecording()}
        return START_NOT_STICKY
    }
    private fun finishRecording(){if(stopping)return;stopping=true;handler.removeCallbacks(timeout);var keep=recording
        try{if(recording)recorder?.stop()}catch(_: Exception){keep=false};recording=false;try{recorder?.reset()}catch(_: Exception){};try{recorder?.release()}catch(_: Exception){};recorder=null
        try{display?.release()}catch(_: Exception){};display=null;try{projection?.stop()}catch(_: Exception){};projection=null;try{file?.close()}catch(_: Exception){};file=null
        uri?.let {try{if(keep){keep=contentResolver.update(it,ContentValues().apply{put(MediaStore.Video.Media.IS_PENDING,0)},null,null)>0}else contentResolver.delete(it,null,null)}catch(_: Exception){keep=false;try{contentResolver.delete(it,null,null)}catch(_: Exception){}}};uri=null
        active=false;ClipBridge.requested=false;ClipBridge.callback?.onState(if(keep)"saved" else "cancelled");stopForeground(STOP_FOREGROUND_REMOVE);stopSelf()
    }
    override fun onTaskRemoved(root: Intent?){finishRecording();super.onTaskRemoved(root)}
    override fun onDestroy(){finishRecording();super.onDestroy()}
}
