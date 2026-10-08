from pathlib import Path
p=Path('Assets/ARRift/Runtime/ARBattlefield.cs');s=p.read_text(encoding='utf-8-sig')
s=s.replace('public float Scale=>placement.settings.Scale;', 'public float Scale=>placement.ActualScale;')
s=s.replace('public bool UserPaused;', 'public bool UserPaused; public bool MenuPaused; public bool AnchorLost {get;private set;} float anchorNoneSince=-1;')
a=s.index('            var boundary=plane.boundary;');b=s.index('            ground.GetComponent<MeshFilter>()',a)
s=s[:a]+'''            const int segments=64;
            var v=new Vector3[segments+1];var triangles=new int[segments*3];v[0]=new Vector3(0,.005f,0);
            for(int i=0;i<segments;i++){float angle=i*Mathf.PI*2/segments;v[i+1]=new Vector3(Mathf.Cos(angle)*extent,.005f,Mathf.Sin(angle)*extent);triangles[i*3]=0;triangles[i*3+1]=(i+1)%segments+1;triangles[i*3+2]=i+1;}
            mesh=new Mesh{name="AR fixed navigation disc",vertices=v,triangles=triangles};mesh.RecalculateNormals();
'''+s[b:]
s=s.replace('bool tracking=ARSession.state==ARSessionState.SessionTracking&&(Root==null||Root.parent.GetComponent<ARAnchor>().trackingState==TrackingState.Tracking);', '''bool anchorNone=placement.Anchor==null||placement.Anchor.trackingState==TrackingState.None;
            if(anchorNone){if(anchorNoneSince<0)anchorNoneSince=Time.unscaledTime;}else anchorNoneSince=-1;
            AnchorLost=Root!=null&&anchorNoneSince>=0&&Time.unscaledTime-anchorNoneSince>2;
            bool tracking=ARSession.state==ARSessionState.SessionTracking&&!AnchorLost;''')
s=s.replace('Paused=Root==null||UserPaused||applicationPaused', 'Paused=Root==null||placement.Adjusting||Shrine==null||UserPaused||MenuPaused||applicationPaused')
a=s.index('            if(occlusion!=null){');b=s.index('\n',a);s=s[:a]+s[b:]
p.write_text(s,encoding='utf-8')
