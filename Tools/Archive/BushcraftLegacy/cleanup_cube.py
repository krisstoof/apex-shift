import bpy
import sys

# Remove default cube from all blend files
def cleanup_default_cube():
    """Remove default cube from scene if it exists"""
    if "Cube" in bpy.data.objects:
        cube = bpy.data.objects["Cube"]
        bpy.data.objects.remove(cube, do_unlink=True)
        print("✓ Default cube removed")
    else:
        print("✓ No default cube found")

if __name__ == "__main__":
    cleanup_default_cube()
    bpy.ops.wm.save_mainfile()
    print("✓ File saved")
