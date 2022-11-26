/* author: someone at https://searchcode.com/codesearch/view/49254163/
 * modified by Xymdyx.
 * Desc: source code for a doubly-connected edge list that is much simpler than the Ethereality.DCEL,
 * which is more helpful if building a DCEL from a list of pre-defined edges rather than in a VD,
 * where we sporadically make dangling halfedges and spawn them accordingly.
*/
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Diagnostics;
using System.Linq;

namespace FortuneAlgo
{

    /// <summary>
    /// A doubly connected edge list.
    /// </summary>
    public class DCEL
    {
        private readonly List<Vertex> vertices;
        private readonly List<Face> faces;
        private readonly List<HalfEdge> halfEdges;
        private readonly Vertex infiniteVertex;

        public static Vertex INFINITY = new Vertex(new Vector2(float.PositiveInfinity, float.PositiveInfinity));
 
 	internal DCEL()
 	{
 		infiniteVertex = new Vertex {Position = new Vector2(float.PositiveInfinity, float.PositiveInfinity)};
 		vertices = new List<Vertex> {infiniteVertex};
        faces = new List<Face>();
        halfEdges = new List<HalfEdge>();
    }
 
    internal void Add(Vertex vertex)
    {
            Debug.Assert(vertex != null);

            if(!this.vertices.Contains(vertex))
                vertices.Add(vertex);
    }

    internal void Add(Face face)
    {
            Debug.Assert(face != null);
            faces.Add(face);
    }

    internal void Add(HalfEdge edge1, HalfEdge edge2)
    {
        Debug.Assert(edge1 != null);
        Debug.Assert(edge2 != null);
        Debug.Assert(edge1.Twin == edge2);
        Debug.Assert(edge2.Twin == edge1);
        Debug.Assert(edge1 != edge2);
        halfEdges.Add(edge1);
        halfEdges.Add(edge2);
    }

    /// <summary>
    /// All of the vertices in this doubly connected edge list, including
    /// the infinite vertex.
    /// </summary>
    public IReadOnlyCollection<Vertex> Vertices { get { return vertices; } }

    /// <summary>
    /// All of the faces in this doubly connected edge list.
    /// </summary>
    public IReadOnlyCollection<Face> Faces { get { return faces; } }

    /// <summary>
    /// All of the half edges in this doubly connected edge list.
    /// </summary>
    public IReadOnlyCollection<HalfEdge> HalfEdges { get { return halfEdges; } }

    /// <summary>
    /// A special vertex "at infinity", used to connect together the edges
    /// of any infinite
    /// </summary>
    public Vertex InfiniteVertex { get { return infiniteVertex; } }

    /// <summary>
    /// Gets all the half edges which border the specified face. For each
    /// bordering full edge, only the half edge which faces the specified
    /// face is returned.
    /// </summary>
    /// <param name="face">The face whose bordering edges will be found.</param>
    /// <param name="borderEdges">The collection to which the bordering
    /// half edges will be added to.</param>
    public static void GetBorderEdges(Face face, ICollection<HalfEdge> borderEdges)
    {
        if (face == null) throw new ArgumentNullException("face");
        if (borderEdges == null) throw new ArgumentNullException("borderEdges");
    
        var current = face.Edge;
        do
        {
            if (current == null)
                    throw new ArgumentException("Face's half edges are not a circularly linked list.");
            borderEdges.Add(current);
        } while ((current = current.Next) != face.Edge) ;
    }

    /// <summary>
    /// Gets all the vertices that compose the specified face.
    /// </summary>
    /// <param name="face">The face whose composing vertices will be found.</param>
    /// <param name="composingVertices">The collection to which the
    /// composing vertices will be added to.</param>
    public static void GetComposingVertices(Face face, ICollection<Vertex> composingVertices)
    {
        if (face == null) throw new ArgumentNullException("face");
        if (composingVertices == null) throw new ArgumentNullException("composingVertices");
    
        var current = face.Edge;
        do
        {
            if (current == null)
                    throw new ArgumentException("Face's half edges are not a circularly linked list.");
            composingVertices.Add(current.Origin);
        } while ((current = current.Next) != face.Edge) ;
    }

    /// <summary>
    /// Gets all the faces that share a border with the specified face.
    /// </summary>
    /// <param name="face">The face whose neighboring faces will be found.</param>
    /// <param name="adjacentFaces">The collection to which the neighboring
    /// faces will be added to.</param>
    public static void GetAdjacentFaces(Face face, ICollection<Face> adjacentFaces)
    {
        if (face == null) throw new ArgumentNullException("face");
        if (adjacentFaces == null) throw new ArgumentNullException("adjacentFaces");
    
        var current = face.Edge;
        do
        {
            if (current == null)
                    throw new ArgumentException("Face's half edges are not a circularly linked list.");
            adjacentFaces.Add(current.Twin.Face);
        } while ((current = current.Next) != face.Edge) ;
    }

        /// <summary>
        /// get a list of HalfEdges that don't have a next and/or prev
        /// </summary>
        /// <returns></returns>
        public List<HalfEdge> getUnBoundedHalfEdges()
        {
            List<HalfEdge> unBoundedEdges = null!;
            if (this.halfEdges != null)
                unBoundedEdges = this.halfEdges.Where(x => x.Prev == null || x.Next == null).ToList();
            return unBoundedEdges;
        }

        /// <summary>
        /// determine if the given vertex is shared by multiple edges
        /// if the number of halfedges whose origin is v > 1, then it's shared
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public bool isSharedVertex(Vertex v)
        {
            int heCount = this.halfEdges.FindAll(he => he.Origin == v).Count;
            return  heCount > 1;
        }
    }


    public class HalfEdge
    {
        /// <summary>
        /// The vertex that this half edge originates from.
        /// </summary>
        public Vertex Origin { get; internal set; }

        /// <summary>
        /// The half edge that together with this half edge makes up one full
        /// edge in the graph.
        /// </summary>
        public HalfEdge Twin { get; internal set; }

        /// <summary>
        /// The next half edge around the border of the face.
        /// </summary>
        public HalfEdge Next { get; internal set; }

        /// <summary>
        /// The previous half edge around the border of the face.
        /// </summary>
        public HalfEdge Prev { get; internal set; }

        /// <summary>
        /// The face that this half edge borders. This face is to the "left"
        /// of this half edge.
        /// </summary>
        public Face Face { get; internal set; }

        internal HalfEdge()
        {
            Origin = null!;
            Twin = null!;
            Next = null!;
            Prev = null!;
            Face = null!;
        }

        internal HalfEdge(Vertex origin)
        {
            Origin = origin;
            Twin = null!;
            Next = null!;
            Prev = null!;
            Face = null!;
        }

        /// <summary>
        /// returns the vertex that this halfedge goes to
        /// from its origin.
        /// </summary>
        /// <returns></returns>
        public Vertex getTarget()
        {
            return this.Twin.Origin;
        }

        /// <summary>
        /// returns the normalized ray repping the halfedge
        /// from its origin to its target
        /// </summary>
        /// <returns>
        /// a Vector2 repping the ray origin->target 
        /// DCEL.INFINITY if the target is infinity
        /// </returns>
        public Vector2 getRay()
        {
            if (this.getTarget() == DCEL.INFINITY)
                return DCEL.INFINITY.Position;

            return Vector2.Normalize(this.getTarget().Position - this.Origin.Position);
        }

        /// <summary>
        /// updates an endpoint of this halfedge
        /// </summary>
        /// <param name="toUpdate"> one of two endpoints that define this edge</param>
        /// <param name="updatePos"> the position of the new endpoint that replaces an old one</param>
        /// <returns> 
        /// DCEL.INFINITY if toUpdate isn't this halfedge's origin or target
        /// a reference to the new vertex otherwise
        /// </returns>
        public Vertex updateEndPt(Vertex toUpdate, Vector2 updatePos)
        {
            if (toUpdate != this.Origin && toUpdate != this.getTarget())
                return DCEL.INFINITY;

            HalfEdge leavingHE = (this.Origin == toUpdate) ? this : this.Twin;
            Vertex newEndPt = new Vertex(leavingHE, updatePos);
            leavingHE.Origin = newEndPt;

            return newEndPt;
        }

        public override string ToString()
        {
            return $"{this.Origin.Position}->{this.Twin.Origin.Position}";
        }
    }

    public class Face
    {
            /// <summary>
        /// One of the half edges bordering this face.
        /// </summary>
        public HalfEdge Edge { get; internal set; }

        /// <summary>
        /// The position of the site that defined this face.
        /// </summary>
        public Vector2 Position { get; internal set; }

        /// <summary>
        /// The user-provided data tag given with the site that defined this
        /// face.
        /// </summary>
        public object Tag { get; internal set; }

        internal Face()
        {
            Edge = null!;
            Position = new Vector2();
            Tag = null!;
        }
    }

    public class Vertex
    {
        /// <summary>
        /// One of the half edges that has this site as its origin.
        /// </summary>
        public HalfEdge Leaving { get; internal set; }

        /// <summary>
        /// The vertex's position.
        /// </summary>
        public Vector2 Position { get; internal set; }

        internal Vertex()
        {
            Leaving = null!;
            Position = new Vector2();
        }

        internal Vertex(Vector2 position)
        {
            Leaving = null!;
            Position = position;
        }

        internal Vertex(HalfEdge leaving, Vector2 position)
        {
            Leaving = leaving;
            Position = position;
        }

        public static bool operator ==(Vertex v1, Vertex v2) => v1.Equals(v2);
        public static bool operator !=(Vertex v1, Vertex v2) => !(v1 == v2);
        public override bool Equals(object? obj)
        {
            Vertex? v = obj as Vertex;
            if (v is null)
                return false;
            if (Object.ReferenceEquals(this, v))
                return true;
            if (this.GetType() != v.GetType())
                return false;

            return (this.Position.X == v.Position.X) && (this.Position.Y == v.Position.Y);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return $"Vertex:{this.Position}";
        }
    }
}

