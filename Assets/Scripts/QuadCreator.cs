using UnityEngine;

public static class QuadCreator
{
   public static Mesh CreateQuad(float width = 1f, float height = 1f)
   {
      //TODO: Can we add rotation here?
      var quad = new Mesh();
        
      var vertices = new[]
      {
         new Vector3(0, 0, 0),
         new Vector3(width, 0, 0),
         new Vector3(0, height, 0),
         new Vector3(width, height, 0)
      };
        
      quad.vertices = vertices;
        
      var tris = new[]
      {
         // lower left triangle
         0, 2, 1,
         // upper right triangle
         2, 3, 1
      };
      quad.triangles = tris;
        
      var normals = new[]
      {
         -Vector3.forward,
         -Vector3.forward,
         -Vector3.forward,
         -Vector3.forward
      };
      
      quad.normals = normals;
      
      var uv = new[]
      {
         new Vector2(0, 0),
         new Vector2(1, 0),
         new Vector2(0, 1),
         new Vector2(1, 1)
      };
      
      quad.uv = uv;

      return quad;
   }
}
