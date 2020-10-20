using UnityEngine;
using System.Collections.Generic;

namespace MBaske
{
    public class GraphFactory : MonoBehaviour
    {
        [SerializeField]
        private bool fitBounds;
        [SerializeField]
        private GameObject graphPrefab;
        private List<Graph> graphs;
        private bool HasGraphs => graphs != null;

        public Graph AddGraph(string name, int width = 0, int height = 0)
        {
            if (!HasGraphs)
            {
                graphs = new List<Graph>();
                gameObject.SetActive(true);
            }

            Graph graph = Instantiate(graphPrefab, transform).GetComponent<Graph>();
            graph.transform.name = name;
            graph.Name = name;
            graph.FitBounds = fitBounds;

            if (width > 0)
            {
                graph.Size.x = width;
            }
            if (height > 0)
            {
                graph.Size.y = height;
            }
            
            graph.UpdateLayout();
            graphs.Add(graph);
            return graph;
        }

        private void Update()
        {
            if (HasGraphs && !fitBounds && Input.GetKeyDown(KeyCode.R))
            {
               foreach (var graph in graphs)
               {
                    graph.ResetBounds();
               }
            }
        }

        private void OnValidate()
        {
            if (HasGraphs)
            {
                foreach (var graph in graphs)
                {
                    graph.FitBounds = fitBounds;
                    graph.ResetBounds();
                }
            }
        }
    }
}