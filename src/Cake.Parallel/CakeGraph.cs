using System;
using System.Collections.Generic;
using System.Linq;
using Cake.Core;

namespace Cake.Parallel.Module
{
    public class CakeGraph
    {
        private readonly List<string> _nodes;
        private readonly List<CakeGraphEdge> _edges;

        public CakeGraph()
        {
            _nodes = new List<string>();
            _edges = new List<CakeGraphEdge>();
        }

        public IReadOnlyList<string> Nodes => _nodes;

        public IReadOnlyList<CakeGraphEdge> Edges => _edges;

        public void Add(string node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }
            if (_nodes.Any(x => x == node))
            {
                throw new CakeException("Node has already been added to graph.");
            }
            _nodes.Add(node);
        }

        public void Connect(string start, string end)
        {
            if (start.Equals(end, StringComparison.OrdinalIgnoreCase))
            {
                throw new CakeException("Reflexive edges in graph are not allowed.");
            }
            if (_edges.Any(x => x.Start.Equals(end, StringComparison.OrdinalIgnoreCase)
                                && x.End.Equals(start, StringComparison.OrdinalIgnoreCase)))
            {
                throw new CakeException("Unidirectional edges in graph are not allowed.");
            }
            if (_edges.Any(x => x.Start.Equals(start, StringComparison.OrdinalIgnoreCase)
                                && x.End.Equals(end, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }
            if (_nodes.All(x => !x.Equals(start, StringComparison.OrdinalIgnoreCase)))
            {
                _nodes.Add(start);
            }
            if (_nodes.All(x => !x.Equals(end, StringComparison.OrdinalIgnoreCase)))
            {
                _nodes.Add(end);
            }
            _edges.Add(new CakeGraphEdge(start, end));
        }

        public bool Exist(string name)
        {
            return _nodes.Any(x => x.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }

    public class CakeGraphEdge
    {
        public string Start { get; set; }
        public string End { get; set; }

        public CakeGraphEdge(string start, string end)
        {
            Start = start;
            End = end;
        }
    }
}
