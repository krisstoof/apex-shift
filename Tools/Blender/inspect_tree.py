"""
Inspect what's inside the leafy_tree.blend file
"""
import bpy

# Load the file
blend_file = r"C:\Users\kriss\apex-shift\Assets\_Project\Art\Bushcraft\Source\Blend\leafy_tree_stylized.blend"
bpy.ops.wm.open_mainfile(filepath=blend_file)

print("\n" + "="*60)
print("LEAFY TREE ANALYSIS")
print("="*60)

# Check scene objects
scene = bpy.context.scene
print(f"\nScene name: {scene.name}")
print(f"Total objects in scene: {len(bpy.data.objects)}")

# List all objects with details
print("\nObjects in scene:")
for obj in bpy.data.objects:
    if obj.type == 'MESH':
        vert_count = len(obj.data.vertices)
        face_count = len(obj.data.polygons)
        tri_count = sum(len(f.vertices) - 2 for f in obj.data.polygons)
        materials = [m.name for m in obj.data.materials if m]
        print(f"  📦 {obj.name:30} | {vert_count:4} verts | {face_count:4} faces | {tri_count:5} tris | materials: {materials}")
    else:
        print(f"  {obj.name:30} ({obj.type})")

# Check if it's just a single object or multiple
mesh_objects = [o for o in bpy.data.objects if o.type == 'MESH']
print(f"\nTotal mesh objects: {len(mesh_objects)}")

# Check total structure
if mesh_objects:
    total_tris = sum(sum(len(f.vertices) - 2 for f in o.data.polygons) for o in mesh_objects)
    print(f"Total triangles across all objects: {total_tris}")
    
# Check materials
print(f"\nMaterials in file: {len(bpy.data.materials)}")
for mat in bpy.data.materials:
    print(f"  - {mat.name}")

print("\n" + "="*60)
