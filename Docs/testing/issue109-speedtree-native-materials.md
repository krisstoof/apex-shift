# Issue #109 native SpeedTree reference verification

2026-10-05, Unity 6000.6.2f1 / URP 17.6, SpeedTree Modeler 10.2.0.

**ApexShift_TropicalTree_Test01 is the REFERENCE SPEEDTREE ASSET for native export, import, materials, scale and LOD setup.** Native visual QA passes. Animated wind is not approved: the source/export has all five motion modules disabled. This does not approve world placement or complete the 26-model production batch.

## Blue/red preview: cause and correction

The earlier immediate batch preview was a false failure. A controlled run reproduced the blue first `SubmitRenderRequest`; the next native Game-camera request rendered brown bark and green foliage with exactly the same model, textures and embedded materials. No material properties, keywords, shader or debug settings changed between those two requests. Subsequent immediate requests also produced intermittent empty frames, making that capture method unsuitable as an acceptance check. All Rendering Debugger modes were inactive. The shader graph in the main and isolated projects has the same SHA256.

The correction is to validate normal Play Mode camera frames after initialization, using a persistent camera target texture and restoring `RenderTexture.active` after readback. The successful run captured 11 images over 781 frames, waiting 60 frames after each LOD/lighting/pipeline change. The exact internal Unity cause of the first-request artifact was not isolated; a shader/importer incompatibility or incorrect exported color map was not demonstrated. A one-frame cold batch capture must not be used to reject this asset.

There was **no tree material replacement or material repair**. Both embedded materials still use `Universal Render Pipeline/Nature/SpeedTree9_URP`, from the installed URP package. The ground alone uses a separate URP/Lit material. No temporary Lit tree is used as evidence.

## Native material results

- Bark: natural brown texture, normal detail, no strong blue/red tint, shadow casting and receiving visible.
- Foliage: tropical green, alpha cutout visible around leaves, front/back lighting responds to the directional light; subsurface map is resolved and its native toggle is enabled. Backlighting was captured. No quantitative transmission calibration is claimed.
- Both materials: white `_Color`, hue variation disabled, normal and extra maps enabled, render queue 2450, `_AlphaClipThreshold = 0.1`. The shader uses `Cull Off`; `Backface_Normal_Mode = 0` selects flipped backface normals. Material feature toggles are graph float properties; empty legacy keyword arrays do not mean missing normal/subsurface features. Wind keywords/toggles are disabled, consistently with native wind data.
- Seven runtime maps: color and subsurface maps use sRGB; normal maps are NormalMap imports with sRGB off; Extra maps are linear, packed smoothness R / metallic G / AO B. Mipmaps are enabled; color-map alpha coverage is preserved at 0.1.
- Source leaf PNGs remain 2048 square. Four runtime leaf imports now have max size 1024 and read back as 1024 square. Bark maps remain 256 by 1024. SPM, ST9 and PNG content were not regenerated or edited.
- Native importer: `UnityEditor.SpeedTree.Importer.SpeedTree9Importer`, embedded material subassets, no external material remap.

## Unity verification

Open **only** `Assets/_Project/Art/Vegetation/SpeedTree/Test/QA/ApexShift_TropicalTree_Test01_NativeQA.unity` in Unity, then press Play. The scene contains one native reference-tree instance, plane, directional light, camera and WindZone. Its inherited Rigidbody is made kinematic on this scene instance so the tree stays grounded. The model import settings and RuntimeWorld are unchanged.

Identity tree transform; LOD0 bounds 4.787 x 9.486 x 5.180 m, minimum Y approximately -0.002 m.

| LOD | Vertices | Triangles | Screen-height threshold |
| --- | ---: | ---: | ---: |
| 0 | 2328 | 2060 | 0.50 |
| 1 | 1371 | 1177 | 0.25 |
| 2 | 544 | 497 | 0.01 |

All three forced LODs and automatic selection on either side of the first two thresholds were captured. Native CrossFade / animated crossfading is enabled; no billboard is present. Stills verify LOD appearance and selection, rather than a recorded temporal fade sequence. PC deferred and Mobile forward pipelines both rendered correctly with native materials, cutout and shadows; no pink or blue/red failure appeared in stabilized Play frames.

WindZone was active. The before/after renders separated by 180 frames have identical SHA256 (`4ff2dd7f9e7aa8a70d5938fb71b6e0eb64b7ec0329a88c89ee92273a5156b1d1`). The genuine SpeedTreeWindAsset contains `m_bDoShared`, `m_bDoBranch1`, `m_bDoBranch2`, `m_bDoRipple`, `m_bDoShimmer` all zero, and both materials have all `_WIND_*` toggles zero. Wind is disabled in this export, not a verified animated feature. The MCP adapter lacks Fan authoring; enabling importer wind alone cannot supply missing motion configuration.

## Evidence and warnings

Repository screenshots (actual native Play Mode renders):

- [PC LOD0, cutout and shadows](issue109-speedtree-native-qa/play-lod0.png)
- [LOD1](issue109-speedtree-native-qa/play-lod1.png)
- [LOD2](issue109-speedtree-native-qa/play-lod2.png)
- [Backlighting](issue109-speedtree-native-qa/play-backlight.png)
- [Mobile forward](issue109-speedtree-native-qa/play-mobile.png)

Machine readback and approval scope: `Assets/_Project/Art/Vegetation/SpeedTree/Test/QA/ApexShift_TropicalTree_Test01_NativeQA.json`. Complete diagnostic scripts, cold/warm control frames, importer/material/wind dumps and Play log: ignored `Logs/Issue109NativeQA/` and `Logs/Issue106Project/Assets/Editor/NativeSpeedTree*QA.cs`.

Warnings: Unity logs an expected legacy SPM importer warning because the preserved editable modern SPM contains no Unity runtime data; import the ST9. Source textures remain installed SpeedTree sample content under the recorded installed license/EULA, not newly owned textures. The native graph shader has an implicit vector-truncation compiler warning; no shader compilation error or unsupported-shader result was observed. Animated wind remains a separate authoring requirement. No mass-density performance or collider approval is inferred.

RuntimeWorld, biome generation, current vegetation, registries and the production manifest were not changed. No further trees were generated.

## Production texture policy

| Vegetation class | Maximum runtime texture dimension |
| --- | --- |
| Hero / landmark | Up to 2048 |
| Large canopy | 1024–2048, according to visual importance |
| Regular tree | 1024 preferred |
| Sapling / shrub | 512–1024 |
| Fern / ground cover | 512–1024 |

Keep full-resolution editable source maps. Set runtime limits with Unity TextureImporter max-size settings, consistently across each atlas's color, normal, extra and subsurface maps. Preserve linear imports for data maps and alpha coverage for foliage color maps. Override platform limits deliberately; regenerating the tree solely to reduce a 2048 source atlas is unnecessary.
