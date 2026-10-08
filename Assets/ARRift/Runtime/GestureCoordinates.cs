using UnityEngine;
namespace CampusRift.AR
{
    public static class GestureCoordinates
    {
        // MediaPipe projects landmarks back into the original CPU input image. The rotation
        // used for inference must NOT be applied again when the AR display transform exists.
        public static Vector2 Viewport(GestureFrame frame,Vector2 image)
        {
            if(frame.screenCoordinates)return new Vector2(image.x,1-image.y);
            if(frame.hasDisplayMatrix)
            {
                var m=frame.displayMatrix;float x=image.x-m.m20,y=1-image.y-m.m21;
                float determinant=m.m00*m.m11-m.m10*m.m01;
                if(Mathf.Abs(determinant)>.00001f)return new Vector2((x*m.m11-y*m.m10)/determinant,(y*m.m00-x*m.m01)/determinant);
            }
            Vector2 rotated=frame.rotation==90?new Vector2(1-image.y,image.x):frame.rotation==180?Vector2.one-image:frame.rotation==270?new Vector2(image.y,1-image.x):image;
            bool portrait=frame.rotation==90||frame.rotation==270;float sourceAspect=portrait?(float)frame.height/Mathf.Max(1,frame.width):(float)frame.width/Mathf.Max(1,frame.height);float targetAspect=(float)Screen.width/Mathf.Max(1,Screen.height);
            if(sourceAspect>targetAspect)rotated.x=(rotated.x-.5f)*sourceAspect/targetAspect+.5f;else rotated.y=(rotated.y-.5f)*targetAspect/sourceAspect+.5f;
            return new Vector2(rotated.x,1-rotated.y);
        }
        public static Vector2 Palm(GestureFrame f)
        {if(f.landmarks==null||f.landmarks.Length!=63)return new Vector2(.5f,.5f);Vector2 p=Vector2.zero;foreach(int i in new[]{0,5,9,13,17})p+=new Vector2(f.landmarks[i*3],f.landmarks[i*3+1]);return Viewport(f,p/5);}
    }
}
