var result=new System.Collections.Generic.List<object>();
for(int z=-50;z<=20;z+=10){UnityEngine.AI.NavMeshHit nav;bool on=UnityEngine.AI.NavMesh.SamplePosition(new UnityEngine.Vector3(0,0,z),out nav,3,UnityEngine.AI.NavMesh.AllAreas);UnityEngine.RaycastHit hit;bool floor=UnityEngine.Physics.Raycast(new UnityEngine.Vector3(0,20,z),UnityEngine.Vector3.down,out hit,25);result.Add(new{z=z,on=on,nav=nav.position.ToString(),floor=floor,support=floor?hit.collider.name:"none",point=hit.point.ToString()});}
return result;
