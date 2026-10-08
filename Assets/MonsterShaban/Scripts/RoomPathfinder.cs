using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CampusRift.Monsters
{
    public static class RoomPathfinder
    {
        struct OpenEntry { public int Node; public float Score; }
        static void Push(List<OpenEntry> heap,int node,float score)
        {
            var entry=new OpenEntry{Node=node,Score=score};int index=heap.Count;heap.Add(entry);
            while(index>0){int parent=(index-1)/2;if(heap[parent].Score<=score)break;heap[index]=heap[parent];index=parent;}
            heap[index]=entry;
        }
        static int Pop(List<OpenEntry> heap)
        {
            int node=heap[0].Node;var last=heap[heap.Count-1];heap.RemoveAt(heap.Count-1);if(heap.Count==0)return node;
            int index=0;
            while(index*2+1<heap.Count)
            {
                int child=index*2+1;if(child+1<heap.Count && heap[child+1].Score<heap[child].Score)child++;
                if(last.Score<=heap[child].Score)break;heap[index]=heap[child];index=child;
            }
            heap[index]=last;return node;
        }
        public static bool Find(RoomGraph graph,Vector3 from,Vector3 evidence,List<Vector3> route)
        { float distance;return Find(graph,from,evidence,route,out distance); }

        public static bool Find(RoomGraph graph,Vector3 from,Vector3 evidence,List<Vector3> route,out float length,int blockedFrom=-1,int blockedTo=-1)
        {
            route.Clear();length=0;if(graph==null || graph.Nodes.Length==0)return false;
            float startDistance,endDistance;
            int start=Attach(graph,from,out startDistance),end=Attach(graph,evidence,out endDistance);
            if(start<0 || end<0)return false;
            var nodes=new List<int>();
            if(!FindNodes(graph,start,end,nodes,out length,blockedFrom,blockedTo))return false;
            foreach(int node in nodes)route.Add(graph.Nodes[node].WorldPosition);
            route.Add(evidence);length+=startDistance+endDistance;return true;
        }
        // A* minimizes weighted cost; physical route length is kept separately for honest ETA.
        public static bool FindNodes(RoomGraph graph,int start,int end,List<int> route,out float length,int blockedFrom=-1,int blockedTo=-1)
        {
            route.Clear();length=0;
            if(graph==null || start<0 || end<0 || start>=graph.Nodes.Length || end>=graph.Nodes.Length)return false;
            int count=graph.Nodes.Length;
            var cost=new float[count];var metres=new float[count];var previous=new int[count];var closed=new bool[count];
            for(int i=0;i<count;i++){cost[i]=float.PositiveInfinity;previous[i]=-1;}
            cost[start]=0;var open=new List<OpenEntry>();Push(open,start,0);
            while(open.Count>0)
            {
                int current=Pop(open);if(closed[current])continue;
                if(current==end)
                {
                    for(int node=end;node>=0;node=previous[node])route.Add(node);
                    route.Reverse();length=metres[end];return true;
                }
                closed[current]=true;
                foreach(var edge in graph.Nodes[current].Neighbors)
                {
                    if(edge.Neighbor<0 || edge.Neighbor>=count || edge.Cost<0)continue;
                    if((current==blockedFrom && edge.Neighbor==blockedTo) || (current==blockedTo && edge.Neighbor==blockedFrom))continue;
                    float next=cost[current]+edge.Cost;if(next>=cost[edge.Neighbor])continue;
                    cost[edge.Neighbor]=next;previous[edge.Neighbor]=current;
                    metres[edge.Neighbor]=metres[current]+(edge.Distance>0?edge.Distance:edge.Cost);
                    Push(open,edge.Neighbor,next+Vector3.Distance(graph.Nodes[edge.Neighbor].WorldPosition,graph.Nodes[end].WorldPosition));
                }
            }
            return false;
        }
        public static int Attach(RoomGraph graph,Vector3 point,out float distance)
        {
            distance=0;var candidates=new List<int>();
            for(int i=0;i<graph.Nodes.Length;i++)
                if(Mathf.Abs(graph.Nodes[i].WorldPosition.y-point.y)<1.5f && (graph.Nodes[i].WorldPosition-point).sqrMagnitude<900)candidates.Add(i);
            candidates.Sort((a,b)=>(graph.Nodes[a].WorldPosition-point).sqrMagnitude.CompareTo((graph.Nodes[b].WorldPosition-point).sqrMagnitude));
            var path=new NavMeshPath();
            for(int i=0;i<Mathf.Min(12,candidates.Count);i++)
            {
                int node=candidates[i];
                if(!NavMesh.CalculatePath(point,graph.Nodes[node].WorldPosition,NavMesh.AllAreas,path) || path.status!=NavMeshPathStatus.PathComplete)continue;
                var corners=path.corners;for(int j=1;j<corners.Length;j++)distance+=Vector3.Distance(corners[j-1],corners[j]);return node;
            }
            return -1;
        }
    }
}
