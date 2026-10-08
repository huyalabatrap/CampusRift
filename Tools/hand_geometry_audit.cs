var skill=UnityEngine.Object.FindAnyObjectByType<CampusRift.Skills.GiantHandSkill>();
var owner=new UnityEngine.GameObject("Temporary geometry audit origin");
var rows=new System.Collections.Generic.List<object>();int valid=0;
try
{
    foreach(var link in UnityEngine.Object.FindObjectsByType<Unity.AI.Navigation.NavMeshLink>().Take(12))
    {
        var a=link.startTransform!=null?link.startTransform.position:link.transform.TransformPoint(link.startPoint);
        var b=link.endTransform!=null?link.endTransform.position:link.transform.TransformPoint(link.endPoint);
        var midpoint=(a+b)*.5f;
        owner.transform.position=a+UnityEngine.Vector3.up*.12f;
        UnityEngine.RaycastHit ground;
        if(!skill.Targeting.Cast(midpoint+UnityEngine.Vector3.up*.8f,UnityEngine.Vector3.down,2,owner.transform,out ground))continue;
        var result=skill.Targeting.Evaluate(ground.point,owner.transform,skill.config);if(result.valid)valid++;
        rows.Add(new{name=link.name,point=ground.point.ToString(),result.valid,result.reason,result.visualScale,result.height});
    }
}
finally{UnityEngine.Object.Destroy(owner);}
return new{valid,samples=rows};
