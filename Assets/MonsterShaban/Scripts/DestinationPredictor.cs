using System;
using System.Collections.Generic;
using UnityEngine;

namespace CampusRift.Monsters
{
    [Serializable] public sealed class DestinationCandidate
    {
        public int Node;
        public Vector3 Position;
        public string Room;
        public float DirectionScore, DistanceScore, HistoryScore, TopologyScore, VisibilityScore;
        public float Score => DirectionScore*.4f+DistanceScore*.2f+HistoryScore*.1f+TopologyScore*.2f+VisibilityScore*.1f;
    }
    // Static topology plus observed motion, with several competing destination hypotheses.
    public static class DestinationPredictor
    {
        public static void Build(RoomGraph graph,Vector3 evidence,Vector3 velocity,float confidence,
            MonsterAIConfig config,MonsterPerception perception,List<DestinationCandidate> output)
        {
            output.Clear();if(graph==null || velocity.sqrMagnitude<1 || confidence<.2f)return;
            Vector3 heading=Vector3.ProjectOnPlane(velocity,Vector3.up).normalized;
            float preferredDistance=Mathf.Clamp(velocity.magnitude*2.5f,7,config.InterceptionRange);
            for(int i=0;i<graph.Nodes.Length;i++)
            {
                var node=graph.Nodes[i];var delta=node.WorldPosition-evidence;float distance=delta.magnitude;
                if(distance<3 || distance>config.InterceptionRange || Mathf.Abs(delta.y)>6)continue;
                float alignment=Vector3.Dot(heading,Vector3.ProjectOnPlane(delta,Vector3.up).normalized);
                if(alignment<.25f)continue;
                bool stair=node.StairEntry || node.StairExit || node.Kind==RoomNodeKind.StairLanding;
                if(Mathf.Abs(delta.y)>1.6f && !stair)continue;
                float strategic=stair?1:node.Kind==RoomNodeKind.Exit?.95f:node.Kind==RoomNodeKind.Junction?.85f:node.Kind==RoomNodeKind.Door?.65f:.35f;
                float scoreDirection=(alignment+1)*.5f;
                float scoreDistance=Mathf.Exp(-Mathf.Abs(distance-preferredDistance)/preferredDistance);
                float history=confidence;
                float preliminary=scoreDirection*.4f+scoreDistance*.2f+history*.1f+strategic*.2f;
                int worst=-1;float minimum=float.PositiveInfinity;bool duplicate=false;
                for(int c=0;c<output.Count;c++)
                {
                    if((output[c].Position-node.WorldPosition).sqrMagnitude<9){duplicate=true;break;}
                    if(output[c].Score<minimum){minimum=output[c].Score;worst=c;}
                }
                if(duplicate || (output.Count>=config.DestinationCandidateCount && preliminary+.1f<=minimum))continue;
                bool visible=perception!=null && perception.ClearLine(evidence+Vector3.up*1.1f,node.WorldPosition+Vector3.up*1.1f,perception.player!=null?perception.player.transform:null);
                var candidate=new DestinationCandidate{Node=i,Position=node.WorldPosition,Room=node.RoomID,DirectionScore=scoreDirection,
                    DistanceScore=scoreDistance,HistoryScore=history,TopologyScore=strategic,VisibilityScore=visible?1:.3f};
                if(output.Count<config.DestinationCandidateCount)output.Add(candidate);
                else if(candidate.Score>minimum)output[worst]=candidate;
            }
            output.Sort((a,b)=>b.Score.CompareTo(a.Score));
        }
    }
}
