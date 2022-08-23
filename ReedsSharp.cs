using System;
using System.Collections;
using Godot;
using System.Collections.Generic;
using Godot.Collections;
using NodeExtensions;
using Object = Godot.Object;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable once ClassNeverInstantiated.Global
public class ReedsSharp : Node
{
    /// <summary> For internal use. </summary>
    public static ReedsSharp Singleton { get; private set; }

    private ImmediateGeometry _immediateGeometry;
    private Spatial _space3D;
    private Node2D _space2D;
    
    private readonly List<Debug.Line> _linesToDraw = new List<Debug.Line>();
    private SpatialMaterial _debugSpatialMat;

    private PhysicsDirectSpaceState _lastSpaceState;
    private Physics2DDirectSpaceState _lastSpaceState2D;
    
    private readonly BoxShape _boxOverlapShape = new BoxShape();
    private readonly SphereShape _sphereOverlapShape = new SphereShape();
    private readonly PhysicsShapeQueryParameters _queryShapeParams = new PhysicsShapeQueryParameters();

    public override void _Ready()
    {
        if (Singleton != null)
        {
            QueueFree();
            return;
        }

        Singleton = this;

        _space2D = new Node2D();
        _space3D = new Spatial();
        _debugSpatialMat = new SpatialMaterial();
        _debugSpatialMat.VertexColorUseAsAlbedo = true;
        _immediateGeometry = new ImmediateGeometry();
        _immediateGeometry.MaterialOverride = _debugSpatialMat;
        
        AddChild(_immediateGeometry);
        AddChild(_space2D);
        AddChild(_space3D);
        
        _lastSpaceState = _space3D.GetWorld().DirectSpaceState;
        _lastSpaceState2D = _space2D.GetWorld2d().DirectSpaceState;
    }

    public override void _Process(float delta)
    {
        DrawDebug();
    }
    
    public override void _PhysicsProcess(float delta)
    {
        _lastSpaceState = _space3D.GetWorld().DirectSpaceState;
        _lastSpaceState2D = _space2D.GetWorld2d().DirectSpaceState;
    }

    public void AddDebugLine(Debug.Line line)
    {
        _linesToDraw.Add(line);
    }

    public void AddWireCube(Debug.WireCuboid wireCuboid)
    {
        _linesToDraw.AddRange(wireCuboid.Lines);
    }

    public void AddGrid2D(Debug.Grid2D grid2d)
    {
        _linesToDraw.AddRange(grid2d.Lines);
    }

    public bool RayCastQuery3D(Vector3 start, Vector3 direction, float distance, out Physics.RayCastHit hit,
        Godot.Collections.Array exclude = null, uint collisionMask = 2147483647U, bool collideWithBodies = true,
        bool collideWithAreas = false)
    {
        hit = new Physics.RayCastHit();
        if (_lastSpaceState == null) 
            return false;
        
        var result = _lastSpaceState.IntersectRay(start, start + direction.Normalized() * distance, exclude,
            collisionMask, collideWithBodies, collideWithAreas);
        
        if (result.Count < 1)
            return false;

        hit = new Physics.RayCastHit(result);
        return hit.Collider != null;
    }
    
    public bool RayCastQuery2D(Vector2 start, Vector2 direction, float distance, out Physics.RayCastHit2D hit,
        Godot.Collections.Array exclude = null, uint collisionMask = 2147483647U, bool collideWithBodies = true,
        bool collideWithAreas = false)
    {
        hit = new Physics.RayCastHit2D();
        if (_lastSpaceState2D == null) 
            return false;
        
        var result = _lastSpaceState2D.IntersectRay(start, start + direction.Normalized() * distance, exclude,
            collisionMask, collideWithBodies, collideWithAreas);
        
        if (result.Count < 1) 
            return false;
        
        hit = new Physics.RayCastHit2D(result);
        return hit.Collider != null;
    }

    public bool OverlapBox(Vector3 extents, out Physics.OverlapResult[] results, int maxQueries = 32,
        Godot.Collections.Array toExclude = null, uint collisionMask = 2147483647U, bool overlapBodies = true,
        bool overlapAreas = true)
    {
        if (toExclude == null)
        {
            toExclude = new Godot.Collections.Array();
        }
        
        _boxOverlapShape.Extents = extents;
        _queryShapeParams.SetShape(_boxOverlapShape);
        _queryShapeParams.Exclude = toExclude;
        _queryShapeParams.CollideWithAreas = overlapAreas;
        _queryShapeParams.CollideWithBodies = overlapBodies;
        _queryShapeParams.CollisionMask = collisionMask;

        var overlapResults = _lastSpaceState.IntersectShape(_queryShapeParams, maxQueries);
        results = new Physics.OverlapResult[Math.Min(overlapResults.Count, maxQueries)];
        if (overlapResults.Count < 1)
            return false;
        
        Physics.OverlapResult.FillResultsNonAlloc(overlapResults, ref results);
        return true;
    }
    
    public bool OverlapSphere(float radius, out Physics.OverlapResult[] results, int maxQueries = 32,
        Godot.Collections.Array toExclude = null, uint collisionMask = 2147483647U, bool overlapBodies = true,
        bool overlapAreas = true)
    {
        if (toExclude == null)
        {
            toExclude = new Godot.Collections.Array();
        }
        
        _sphereOverlapShape.Radius = radius;
        _queryShapeParams.SetShape(_sphereOverlapShape);
        _queryShapeParams.Exclude = toExclude;
        _queryShapeParams.CollideWithAreas = overlapAreas;
        _queryShapeParams.CollideWithBodies = overlapBodies;
        _queryShapeParams.CollisionMask = collisionMask;

        var overlapResults = _lastSpaceState.IntersectShape(_queryShapeParams, maxQueries);
        results = new Physics.OverlapResult[Math.Min(overlapResults.Count, maxQueries)];
        if (overlapResults.Count < 1)
            return false;
        
        Physics.OverlapResult.FillResultsNonAlloc(overlapResults, ref results);
        return true;
    }

    private void DrawDebug()
    {
        _immediateGeometry.Clear();
        _immediateGeometry.Begin(Mesh.PrimitiveType.Lines);
        foreach (var line in _linesToDraw)
        {
            _immediateGeometry.SetNormal(-Vector3.Forward);
            _immediateGeometry.SetUv(new Vector2(0, 1));
            _immediateGeometry.SetColor(line.Color);
            _immediateGeometry.AddVertex(line.Start);
            _immediateGeometry.SetNormal(-Vector3.Forward);
            _immediateGeometry.SetUv(new Vector2(0, 1));
            _immediateGeometry.AddVertex(line.End);
        }
        _immediateGeometry.End();
        _linesToDraw.Clear();
    }
}

public static class Physics
{
    public static bool Raycast(Vector3 start, Vector3 direction, float range, out RayCastHit hit,
        Godot.Collections.Array exclude = null, uint collisionMask = 2147483647U, bool collideWithBodies = true,
        bool collideWithAreas = false)
    {
        return ReedsSharp.Singleton.RayCastQuery3D(start, direction, range, out hit, exclude, collisionMask, collideWithBodies, collideWithAreas);
    }
    
    public static bool Raycast2D(Vector2 start, Vector2 direction, float range, out RayCastHit2D hit,
        Godot.Collections.Array exclude = null, uint collisionMask = 2147483647U, bool collideWithBodies = true,
        bool collideWithAreas = false)
    {
        return ReedsSharp.Singleton.RayCastQuery2D(start, direction, range, out hit, exclude, collisionMask, collideWithBodies, collideWithAreas);
    }

    public static bool OverlapBox(Vector3 extents, out OverlapResult[] results, int maxQueries = 32,
        Godot.Collections.Array toExclude = null, uint collisionMask = 2147483647U, bool overlapBodies = true,
        bool overlapAreas = true)
    {
        return ReedsSharp.Singleton.OverlapBox(extents, out results, maxQueries, toExclude, collisionMask,
            overlapBodies, overlapAreas);
    }
    
    public static bool OverlapSphere(float radius, out OverlapResult[] results, int maxQueries = 32,
        Godot.Collections.Array toExclude = null, uint collisionMask = 2147483647U, bool overlapBodies = true,
        bool overlapAreas = true)
    {
        return ReedsSharp.Singleton.OverlapSphere(radius, out results, maxQueries, toExclude, collisionMask,
            overlapBodies, overlapAreas);
    }

    public readonly struct OverlapResult
    {
        public readonly Object Collider;
        public readonly int ColliderId;
        public readonly RID RId;
        public readonly int Shape;

        public OverlapResult(IDictionary dictionary)
        {
            Collider = (Object)dictionary["collider"];
            ColliderId = (int)dictionary["collider_id"];
            RId = (RID)dictionary["rid"];
            Shape = (int)dictionary["shape"];
        }

        public static void FillResultsNonAlloc(Godot.Collections.Array source, ref OverlapResult[] results)
        {
            var min = Math.Min(source.Count, results.Length);
            for (var i = 0; i < min; ++i)
            {
                results[i] = new OverlapResult((IDictionary)source[i]);
            }
        }
    }
    
    public readonly struct RayCastHit2D
    {
        public readonly Vector2 Position;
        public readonly Vector2 Normal;
        public readonly Object Collider;
        public readonly int ColliderId;
        public readonly RID RId;
        public readonly int Shape;

        public RayCastHit2D(IDictionary dictionary)
        {
            Position = (Vector2)dictionary["position"];
            Normal = (Vector2)dictionary["normal"];
            Collider = (Object)dictionary["collider"];
            ColliderId = (int)dictionary["collider_id"];
            RId = (RID)dictionary["rid"];
            Shape = (int)dictionary["shape"];
        }
    }
    
    public readonly struct RayCastHit
    {
        public readonly Vector3 Position;
        public readonly Vector3 Normal;
        public readonly Object Collider;
        public readonly int ColliderId;
        public readonly RID RId;
        public readonly int Shape;

        public RayCastHit(IDictionary dictionary)
        {
            Position = (Vector3)dictionary["position"];
            Normal = (Vector3)dictionary["normal"];
            Collider = (Object)dictionary["collider"];
            ColliderId = (int)dictionary["collider_id"];
            RId = (RID)dictionary["rid"];
            Shape = (int)dictionary["shape"];
        }
    } 
    
    // TODO: Rest of physics functions needs to return non-allocating structs
}

public static class Debug
{
    public static void Log(params object[] msg)
    {
        GD.Print(msg);
    }
    
    public static void LogError(params object[] msg)
    {
        GD.PrintErr(msg);
    }

    /// <summary>
    /// Draws a line between 2 points with the given color.
    /// </summary>
    public static void DrawLine(Vector3 start, Vector3 end, Color color)
    {
        ReedsSharp.Singleton.AddDebugLine(new Line(start, end, color));
    }

    /// <summary>
    /// Draws a cuboid with the given dimensions, origin, and color.
    /// </summary>
    public static void DrawWireCuboid(Vector3 origin, Vector3 dimsXYZ, Color color)
    {
        ReedsSharp.Singleton.AddWireCube(new WireCuboid(origin, dimsXYZ, color));
    }

    /// <summary>
    /// Draws A grid extending forward (in GL Coords) and to the right from the given origin.
    /// </summary>
    public static void DrawGrid2D(Vector3 bottomLeft, int cellsX, int cellsZ, float increment, Color color)
    {
        ReedsSharp.Singleton.AddGrid2D(new Grid2D(bottomLeft, cellsX, cellsZ, increment, color));
    }

    public readonly struct Line
    {
        public readonly Vector3 Start;
        public readonly Vector3 End;
        public readonly Color Color;

        public Line(Vector3 start, Vector3 end, Color color)
        {
            Start = start;
            End = end;
            Color = color;
        }
    }

    public readonly struct Grid2D
    {
        public readonly Vector3 BottomLeft;
        public readonly int CellsX;
        public readonly int CellsZ;
        public readonly float Increment;
        public readonly Color Color;
        public readonly Line[] Lines;

        public Grid2D(Vector3 bottomLeft, int cellsX, int cellsZ, float increment, Color color)
        {
            BottomLeft = bottomLeft;
            CellsX = cellsX;
            CellsZ = cellsZ;
            Increment = increment;
            Lines = new Line[cellsX + cellsZ + 2];
            Color = color;
            PopulateLines();
        }

        private void PopulateLines()
        {
            for (var i = 0; i <= CellsX; ++i)
            {
                var start = new Vector3(i * Increment, 0, 0) + BottomLeft;
                var end = new Vector3(i * Increment, 0, -CellsZ * Increment) + BottomLeft;
                Lines[i] = new Line(start, end, Color);
            }
            for (var i = CellsX + 1; i < Lines.Length; ++i)
            {
                var rung = i - CellsX - 1;
                var start = new Vector3(0, 0, -rung * Increment) + BottomLeft;
                var end = new Vector3(CellsZ * Increment, 0, -rung * Increment) + BottomLeft;
                Lines[i] = new Line(start, end, Color);
            }
        }
    }

    public readonly struct WireCuboid
    {
        public readonly Vector3 Origin;
        public readonly Vector3 DimXYZ;
        public readonly Color Color;
        public readonly Line[] Lines;

        public WireCuboid(Vector3 origin, Vector3 dimXYZ, Color color)
        {
            Origin = origin;
            DimXYZ = dimXYZ;
            Color = color;

            Lines = new Line[12];
            PopulateLines();
        }

        private void PopulateLines()
        {
            // This is a bit gross, but faster.
            
            var halfExtents = DimXYZ / 2f;
            
            // Top Face
            Lines[0] = new Line(
                new Vector3(Origin + new Vector3(halfExtents.x, halfExtents.y, halfExtents.z)),
                new Vector3(Origin + new Vector3(-halfExtents.x, halfExtents.y, halfExtents.z)), 
                Color);
            
            Lines[1] = new Line(
                new Vector3(Origin + new Vector3(halfExtents.x, halfExtents.y, -halfExtents.z)),
                new Vector3(Origin + new Vector3(-halfExtents.x, halfExtents.y, -halfExtents.z)), 
                Color);

            Lines[2] = new Line(
                new Vector3(Origin + new Vector3(halfExtents.x, halfExtents.y, halfExtents.z)),
                new Vector3(Origin + new Vector3(halfExtents.x, halfExtents.y, -halfExtents.z)), 
                Color);
            
            Lines[3] = new Line(
                new Vector3(Origin + new Vector3(-halfExtents.x, halfExtents.y, halfExtents.z)),
                new Vector3(Origin + new Vector3(-halfExtents.x, halfExtents.y, -halfExtents.z)), 
                Color);
            
            // Bottom Face
            Lines[4] = new Line(
                new Vector3(Origin + new Vector3(halfExtents.x, -halfExtents.y, halfExtents.z)),
                new Vector3(Origin + new Vector3(-halfExtents.x, -halfExtents.y, halfExtents.z)), 
                Color);
            
            Lines[5] = new Line(
                new Vector3(Origin + new Vector3(halfExtents.x, -halfExtents.y, -halfExtents.z)),
                new Vector3(Origin + new Vector3(-halfExtents.x, -halfExtents.y, -halfExtents.z)), 
                Color);

            Lines[6] = new Line(
                new Vector3(Origin + new Vector3(halfExtents.x, -halfExtents.y, halfExtents.z)),
                new Vector3(Origin + new Vector3(halfExtents.x, -halfExtents.y, -halfExtents.z)), 
                Color);
            
            Lines[7] = new Line(
                new Vector3(Origin + new Vector3(-halfExtents.x, -halfExtents.y, halfExtents.z)),
                new Vector3(Origin + new Vector3(-halfExtents.x, -halfExtents.y, -halfExtents.z)), 
                Color);
            
            // Front Pair
            Lines[8] = new Line(
                new Vector3(Origin + new Vector3(halfExtents.x, halfExtents.y, halfExtents.z)),
                new Vector3(Origin + new Vector3(halfExtents.x, -halfExtents.y, halfExtents.z)), 
                Color);
            
            Lines[9] = new Line(
                new Vector3(Origin + new Vector3(-halfExtents.x, halfExtents.y, halfExtents.z)),
                new Vector3(Origin + new Vector3(-halfExtents.x, -halfExtents.y, halfExtents.z)), 
                Color);
            
            // Back Pair
            Lines[10] = new Line(
                new Vector3(Origin + new Vector3(halfExtents.x, halfExtents.y, -halfExtents.z)),
                new Vector3(Origin + new Vector3(halfExtents.x, -halfExtents.y, -halfExtents.z)), 
                Color);
            
            Lines[11] = new Line(
                new Vector3(Origin + new Vector3(-halfExtents.x, halfExtents.y, -halfExtents.z)),
                new Vector3(Origin + new Vector3(-halfExtents.x, -halfExtents.y, -halfExtents.z)), 
                Color);
        }
    }
}

namespace NodeExtensions
{
    // Stuff just to reduce the clutter with common operations. Extension properties when?
    
    public static class SpatialExtensions
    {
        public static Vector3 GetPosition(this Spatial self)
        {
            return self.GlobalTransform.origin;
        }
        
        public static void SetPosition(this Spatial self, Vector3 pos)
        {
            self.GlobalTransform = new Transform(self.GlobalTransform.basis, pos);
        }

        public static Quat GetRotation(this Spatial self)
        {
            return self.GlobalTransform.basis.RotationQuat();
        }

        public static void SetRotation(this Spatial self, Quat rot)
        {
            self.GlobalTransform = new Transform(new Basis(rot), self.GlobalTransform.origin);
        }
        
        public static void SetRotation(this Spatial self, Vector3 rot)
        {
            self.GlobalTransform = new Transform(new Basis(rot), self.GlobalTransform.origin);
        }

        public static Vector3 GetLocalPosition(this Spatial self)
        {
            return self.Transform.origin;
        }
        
        public static void SetLocalPosition(this Spatial self, Vector3 pos)
        {
            self.Translation = pos;
        }
        
        public static Quat GetLocalRotation(this Spatial self)
        {
            return self.Transform.basis.RotationQuat();
        }

        public static void SetLocalRotation(this Spatial self, Quat rot)
        {
            self.Transform = new Transform(new Basis(rot), self.Transform.origin);
        }

        public static void SetLocalRotation(this Spatial self, Vector3 rot)
        {
            self.Rotation = rot;
        }

        public static Vector3 Right(this Spatial self)
        {
            return self.Transform.basis.x;
        }

        public static Vector3 Forward(this Spatial self)
        {
            return self.Transform.basis.z;
        }
        
        public static Vector3 Up(this Spatial self)
        {
            return self.Transform.basis.y;
        }
    }
}
