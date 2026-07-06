"""
Open leafy tree and set isometric camera view
"""
import bpy
import math

# Load the file
blend_file = r"C:\Users\kriss\apex-shift\Assets\_Project\Art\Bushcraft\Source\Blend\leafy_tree_stylized.blend"
bpy.ops.wm.open_mainfile(filepath=blend_file)

scene = bpy.context.scene
tree_obj = bpy.data.objects.get("leafy_tree_stylized")

if tree_obj:
    # Focus on tree
    for obj in bpy.data.objects:
        obj.select_set(False)
    tree_obj.select_set(True)
    bpy.context.view_layer.objects.active = tree_obj
    
    # Zoom to object
    bpy.ops.view3d.view_all(center=False)
    
    # Set up isometric camera view
    camera = bpy.data.objects.get("Camera")
    if camera:
        # Position camera for isometric view (45 degrees, front-top-right)
        distance = 6.0
        angle = math.radians(45)
        camera.location = (distance * math.cos(angle), -distance * math.sin(angle), distance * 0.75)
        camera.rotation_euler = (math.radians(75), 0, math.radians(45))
        scene.camera = camera
    
    print(f"✓ Tree loaded: {tree_obj.name}")
    print(f"✓ Polycount: {sum(len(f.vertices)-2 for f in tree_obj.data.polygons)} triangles")
    print(f"✓ Camera positioned for isometric view")
else:
    print("✗ Tree not found")
