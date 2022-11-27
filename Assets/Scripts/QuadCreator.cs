using UnityEngine;

public static class QuadCreator
{
   public static Mesh CreateQuad(float width = 1f, float height = 1f)
   {
      float half = width / 2f;
      var quad = new Mesh();
        
      var vertices = new[]
      {
         new Vector3(-half, 0, 0),
         new Vector3(half, 0, 0),
         new Vector3(-half, height, 0),
         new Vector3(half, height, 0)
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
         Vector3.up, 
         Vector3.up, 
         Vector3.up, 
         Vector3.up
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
