using System;
using UnityEngine;

namespace DoHwa.World
{
    [Serializable]
    public sealed class WorldRegion
    {
        public string title;
        public string climate="봄 / 맑음";
        public bool groundCover=true;
        public Vector2 center;
        public Vector2 routeOffset;
        public float elevation;
        public float relief=12;
        public Color ground=Color.gray;
        public GameObject vegetation;
        public GameObject scenery;
        public GameObject landmark;
        public GameObject pier;
        public GameObject[] roadsideProps=Array.Empty<GameObject>();
        public int vegetationCount=48;
        public bool blossoms;
        public bool timber=true;
        public float lakeRadius;
    }
}




