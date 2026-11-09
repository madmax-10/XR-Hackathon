// Dev B reproducible art/scene authoring, not a shipped runtime dependency.
// Copy to Assets/DevBAuthoring/Editor temporarily; run FinalWorldAuthoring.Build in Unity.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.XR.CoreUtils;
using TMPro;
using SafeShelter;

public static class FinalWorldAuthoring
{
    const string Art = "Assets/SafeShelter/CrowdArt";
    const string ScenePath = "Assets/Scenes/SafeShelter_Integration.unity";
    static Transform environment, waypoints, decor;
    static Material navy, cream, teal, amber, white, porcelain, ink;
    static T[] All<T>() where T : Component => SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
    static Transform Find(string name) => All<Transform>().Single(t => t.name == name);
    static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    static Transform Empty(string name, Transform parent, Vector3 pos)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false); t.localPosition = pos; return t;
    }
    static Material Mat(string name, Color color)
    {
        string path = Art + "/Materials/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", color); m.SetFloat("_Smoothness", .12f); m.enableInstancing=true; EditorUtility.SetDirty(m); return m;
    }
    static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material, bool solid = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
        go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = material;
        if (!solid) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        else go.layer = 9;
        return go;
    }
    static void Label(string name, string words, Transform parent, Vector3 pos, Vector2 size, float font, bool floor = false, float yaw = 0)
    {
        var t = Empty(name, parent, pos);
        t.localRotation = floor ? Quaternion.Euler(90, yaw, 0) : Quaternion.Euler(0, yaw, 0);
        var text = t.gameObject.AddComponent<TextMeshPro>();
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        text.text = words; text.fontSize = font; text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white; text.textWrappingMode = TextWrappingModes.NoWrap;
        text.rectTransform.sizeDelta = size; text.raycastTarget = false;
        text.enableAutoSizing=true;text.fontSizeMin=.8f;text.fontSizeMax=font;text.ForceMeshUpdate();
    }
    static void Sign(string name, string words, Vector3 pos, float width, Material material, float yaw = 0)
    {
        var root = Empty(name, decor, pos); root.localRotation = Quaternion.Euler(0, yaw, 0);
        Box("Panel", root, Vector3.zero, new Vector3(width, .52f, .06f), material);
        Label("Front", words, root, new Vector3(0,0,-.036f), new Vector2(width-.1f,.46f), 3.1f);
        Label("Back", words, root, new Vector3(0,0,.036f), new Vector2(width-.1f,.46f), 3.1f, false, 180);
    }
    static void FloorLabel(string name, string words, Vector3 pos, float width, Material material, float yaw = 0)
    {
        var root = Empty(name, decor, pos); root.localRotation = Quaternion.Euler(0,yaw,0);
        Box("Paint", root, new Vector3(0,.006f,0), new Vector3(width,.008f,.50f), material);
        Label("Legend", words, root, new Vector3(0,.012f,0), new Vector2(width-.06f,.44f), 2.5f, true);
    }
    public static void Build()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        SafeShelter.Editor.IntegrationSetup.Validate();
        // The inherited template's dense fog obscures the 28 m planner overview.
        RenderSettings.fog=false;
        Directory.CreateDirectory(Art + "/Materials"); Directory.CreateDirectory(Art + "/Animation");
        AssetDatabase.Refresh();
        navy=Mat("Sign_Navy",new Color(.035f,.09f,.15f)); cream=Mat("Partition_WarmWhite",new Color(.76f,.80f,.78f));
        teal=Mat("Pantry_Teal",new Color(.02f,.40f,.36f)); amber=Mat("Parking_Amber",new Color(.90f,.51f,.08f));
        white=Mat("Marking_White",new Color(.92f,.94f,.90f)); porcelain=Mat("Porcelain",new Color(.90f,.93f,.94f));
        ink=Mat("Fixture_Dark",new Color(.055f,.075f,.095f));
        environment=Find("Environment");
        foreach (var name in new[]{"CrowdDemo","FinalDemoDecor","Partition_AisleWest_North","Partition_AisleEast_North","CrowdViewerPresence"})
            foreach (var t in All<Transform>().Where(t=>t.name==name).ToArray()) UnityEngine.Object.DestroyImmediate(t.gameObject);
        decor=Empty("FinalDemoDecor",environment,Vector3.zero);
        var crowd=Empty("CrowdDemo",null,Vector3.zero); waypoints=Empty("Waypoints",crowd,Vector3.zero);
        var west=Find("Partition_AisleWest"); west.position=new Vector3(-1.25f,1,-2.275f); west.localScale=new Vector3(.2f,2,7.45f);
        var east=Find("Partition_AisleEast"); east.position=new Vector3(1.25f,1,-1.8f); east.localScale=new Vector3(.2f,2,8.4f);
        Box("Partition_AisleWest_North",environment,new Vector3(-1.25f,1,4.425f),new Vector3(.2f,2,1.15f),west.GetComponent<Renderer>().sharedMaterial,true);
        Box("Partition_AisleEast_North",environment,new Vector3(1.25f,1,4.9f),new Vector3(.2f,2,.2f),east.GetComponent<Renderer>().sharedMaterial,true);
        ReplaceBaselinePrefabs();
        Anchor("AisleGateSouth",0,0,-1.15f); Anchor("AisleGateNorth",0,0,1.15f);
        Anchor("PantryPortal",-1.25f,0,2.65f); Anchor("PantryService",-9.05f,0,.9f);
        Anchor("ToiletPortal",1.25f,0,3.6f); Anchor("ToiletEntry",9.15f,0,6.85f); Anchor("ToiletService",10.35f,0,6.85f);
        Anchor("ClearBedPose",4,.225f,-2);
        for(int i=0;i<8;i++) Anchor($"PantryQueue_{i+1:00}",-8.25f+.8f*i,0,.9f);
        for(int i=0;i<4;i++) Anchor($"ToiletQueue_{i+1:00}",8.35f-.8f*i,0,6.85f);
        for(int i=0;i<12;i++) Anchor($"TransitQueue_{i+1:00}",i%2==0?-.6f:.6f,0,-1.7f-.88f*(i/2));
        Vector2[] starts={new Vector2(-2.2f,-7.15f),new Vector2(2.2f,-7.15f),new Vector2(-3.4f,-7.15f),new Vector2(3.4f,-7.15f),new Vector2(-3.4f,-6.15f),new Vector2(3.4f,-6.15f),new Vector2(-4.6f,-7.15f),new Vector2(4.6f,-7.15f),new Vector2(-2.2f,-6.15f),new Vector2(2.2f,-6.15f),new Vector2(-4.6f,-6.15f),new Vector2(4.6f,-6.15f)};
        for(int i=0;i<12;i++) Anchor($"ResidentStart_{i+1:00}",starts[i].x,0,starts[i].y);
        // Every mark is collider-free so raycasts still resolve to HallFloor.
        foreach(float x in new[]{2.9f,5.1f}) Box("BedParking_Edge",decor,new Vector3(x,.007f,-2),new Vector3(.045f,.01f,1.4f),amber);
        foreach(float z in new[]{-2.7f,-1.3f}) Box("BedParking_Edge",decor,new Vector3(4,.007f,z),new Vector3(2.2f,.01f,.045f),amber);
        FloorLabel("BedParking_Label","BED PARKING",new Vector3(4,0,-2),2.05f,navy);
        Sign("BedParking_Sign","BED PARKING",new Vector3(4,1.8f,-2.95f),2.05f,navy);
        foreach(var p in new[]{new Vector3(-1,0,-.65f),new Vector3(1,0,.65f)})
        {
            Box("KeepClear_CornerTick_X",decor,p+new Vector3(0,.008f,0),new Vector3(.2f,.01f,.035f),amber);
            Box("KeepClear_CornerTick_Z",decor,p+new Vector3(0,.008f,0),new Vector3(.035f,.01f,.2f),amber);
        }
        FloorLabel("SouthAisleView","SOUTH AISLE VIEW",new Vector3(0,0,-2.5f),2.05f,navy);
        FloorLabel("NorthCrossingView","NORTH CROSSING VIEW",new Vector3(0,0,3),2.05f,navy);
        FloorLabel("Pantry_Overview","FOOD PANTRY",new Vector3(-10.05f,0,-2.45f),2.85f,teal);
        Sign("Pantry_Header","FOOD PANTRY",new Vector3(-10,2.35f,2.9f),3.5f,teal);
        Sign("Pantry_AisleSign","FOOD PANTRY  <",new Vector3(-1.26f,2.35f,4.43f),2.7f,teal);
        for(int i=0;i<8;i++)
        {
            float x=-8.25f+.8f*i;
            Box($"PantryLine_{i+1:00}",decor,new Vector3(x,.008f,.9f),new Vector3(.055f,.01f,.55f),teal);
        }
        FloorLabel("Pantry_LineLabel","QUEUE  <",new Vector3(-5.5f,0,1.6f),1.7f,teal);
        var tin=Mat("Food_Tin",new Color(.75f,.29f,.13f)); var grain=Mat("Food_Grain",new Color(.85f,.68f,.35f));
        // Parent table-top decorations to their movable table so editing/reset carries them with it.
        for(int table=1;table<=2;table++)
        {
            var parent=Find($"SupplyTable_{table:00}");
            foreach(var old in parent.Cast<Transform>().Where(t=>t.name=="PantryFood").ToArray()) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var props=Empty("PantryFood",parent,Vector3.zero);
            for(int i=0;i<3;i++) Box("Food_Carton",props,new Vector3(-.35f+i*.32f,.55f,0),new Vector3(.21f,.3f,.22f),i%2==0?grain:tin);
        }
        Sign("Pantry_CounterSign","COLLECT FOOD",new Vector3(-9.35f,1.2f,0),1.5f,teal,90);
        BuildToilets();
        FloorLabel("Medical_Overview","MEDICAL",new Vector3(10,0,-2.45f),2.5f,navy);
        Sign("Medical_Header","+  MEDICAL",new Vector3(10,2.35f,2.9f),3.0f,navy);
        FloorLabel("Exit_Overview","EXIT  ^",new Vector3(0,0,7.2f),2.4f,teal);
        Sign("Exit_Header","EXIT",new Vector3(0,2.55f,7.9f),2.4f,teal);
        // ExitMarker was an extra floor-standing cube; retain its visual, remove its obstruction.
        foreach(var c in Find("ExitMarker").GetComponents<Collider>()) UnityEngine.Object.DestroyImmediate(c);
        BuildPeople(); ConfigureRig(); WireIfAvailable();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Validate(); RenderReview();
    }
    static void Anchor(string name,float x,float y,float z) => Empty(name,waypoints,new Vector3(x,y,z));
    public static void BuildAndPackage()
    {
        Build();
        SafeShelter.Editor.IntegrationSetup.BuildWindows();
    }
    static void ReplaceBaselinePrefabs()
    {
        var layout=All<FurnitureLayout>().Single(); var so=new SerializedObject(layout); var array=so.FindProperty("initialFurniture");
        Check(array.arraySize==22,"Baseline count");
        for(int i=0;i<22;i++)
        {
            var old=(SharedFurniture)array.GetArrayElementAtIndex(i).objectReferenceValue;
            string prefab=i<11?"NetworkBed":i<13?"NetworkSupplyTable":i<17?"NetworkCrate":i==17?"NetworkMedicalTable":"NetworkChair";
            string path="Assets/SafeShelter/Prefabs/"+prefab+".prefab";
            if(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(old.gameObject)==path) continue;
            var next=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            next.name=old.name; next.transform.SetParent(old.transform.parent,false);
            next.transform.SetPositionAndRotation(old.transform.position,old.transform.rotation); next.transform.localScale=Vector3.one;
            next.transform.SetSiblingIndex(old.transform.GetSiblingIndex());
            array.GetArrayElementAtIndex(i).objectReferenceValue=next.GetComponent<SharedFurniture>();
            UnityEngine.Object.DestroyImmediate(old.gameObject);
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    static void BuildToilets()
    {
        var room=Empty("ToiletRoom",decor,Vector3.zero);
        Box("ToiletWall_South",room,new Vector3(10.45f,1,4.55f),new Vector3(2.6f,2,.12f),cream,true);
        Box("ToiletWall_North",room,new Vector3(10.45f,1,7.55f),new Vector3(2.6f,2,.12f),cream,true);
        Box("ToiletWall_East",room,new Vector3(11.75f,1,6.05f),new Vector3(.12f,2,3),cream,true);
        Box("ToiletWall_West",room,new Vector3(9.15f,1,5.35f),new Vector3(.12f,2,1.6f),cream,true);
        Box("ToiletPrivacyDivider",room,new Vector3(10.45f,.9f,5.3f),new Vector3(.10f,1.8f,1.4f),cream,true);
        for(int i=0;i<2;i++)
        {
            float x=9.8f+i*1.3f;
            Box($"Toilet_{i+1:00}_Pedestal",room,new Vector3(x,.18f,5.25f),new Vector3(.36f,.36f,.5f),porcelain,true);
            Box($"Toilet_{i+1:00}_Bowl",room,new Vector3(x,.4f,5.28f),new Vector3(.46f,.16f,.62f),porcelain,true);
            Box($"Toilet_{i+1:00}_Seat",room,new Vector3(x,.49f,5.28f),new Vector3(.38f,.025f,.51f),ink);
            Box($"Toilet_{i+1:00}_SeatInset",room,new Vector3(x,.506f,5.29f),new Vector3(.24f,.01f,.34f),porcelain);
            Box($"Toilet_{i+1:00}_Cistern",room,new Vector3(x,.67f,4.89f),new Vector3(.44f,.48f,.22f),porcelain,true);
            Box($"Toilet_{i+1:00}_Flush",room,new Vector3(x,.92f,4.89f),new Vector3(.06f,.025f,.04f),ink);
        }
        Sign("Toilets_Header","TOILETS",new Vector3(10.45f,2.35f,4.55f),2.5f,navy);
        Sign("Toilets_EntranceSign","TOILETS",new Vector3(9.14f,2.35f,6.85f),1.9f,navy,90);
        Sign("Toilets_AisleSign","TOILETS  >",new Vector3(2.7f,2.35f,5.2f),2.5f,navy);
        FloorLabel("Toilets_Overview","TOILETS",new Vector3(10.45f,0,6.85f),2.3f,navy);
        Box("ToiletThreshold",decor,new Vector3(9.15f,.008f,6.85f),new Vector3(.1f,.01f,1.3f),teal);
        for(int i=0;i<4;i++) Box($"ToiletLine_{i+1:00}",decor,new Vector3(8.35f-.8f*i,.008f,6.85f),new Vector3(.055f,.01f,.55f),teal);
    }
    static void BuildPeople()
    {
        var skinA=Mat("Skin_Warm",new Color(.61f,.36f,.23f)); var skinB=Mat("Skin_Tan",new Color(.86f,.65f,.46f));
        var shirtA=Mat("Clothing_Teal",new Color(.035f,.47f,.46f)); var shirtB=Mat("Clothing_Ochre",new Color(.9f,.48f,.12f));
        var pants=Mat("Clothing_Indigo",new Color(.10f,.16f,.25f));
        var controllerPath=Art+"/Animation/Resident.controller";
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if(controller==null) controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.parameters=new[]{new AnimatorControllerParameter{name="MoveSpeed",type=AnimatorControllerParameterType.Float}};
        var machine=controller.layers[0].stateMachine;
        foreach(var state in machine.states) {var old=state.state.motion;machine.RemoveState(state.state);if(old is BlendTree)UnityEngine.Object.DestroyImmediate(old,true);}
        var idle=MakeClip("Resident_Idle",false); var walk=MakeClip("Resident_Walk",true);
        var blend=new BlendTree{name="Idle to walk (m per second)",blendType=BlendTreeType.Simple1D,blendParameter="MoveSpeed",useAutomaticThresholds=false};
        blend.AddChild(idle,0); blend.AddChild(walk,1.15f); AssetDatabase.AddObjectToAsset(blend,controller);
        var locomotion=machine.AddState("Locomotion"); locomotion.motion=blend; machine.defaultState=locomotion;
        foreach(string variant in new[]{"A","B"})
        {
            var root=Empty("HumanVisual_"+variant,null,Vector3.zero); var skin=variant=="A"?skinA:skinB; var shirt=variant=="A"?shirtA:shirtB;
            var animator=root.gameObject.AddComponent<Animator>(); animator.runtimeAnimatorController=controller; animator.applyRootMotion=false;
            Box("Torso",root,new Vector3(0,1.18f,0),new Vector3(.42f,.53f,.24f),shirt);
            Box("Hips",root,new Vector3(0,.90f,0),new Vector3(.34f,.18f,.23f),pants);
            Box("Neck",root,new Vector3(0,1.48f,0),new Vector3(.13f,.13f,.13f),skin);
            Box("Head",root,new Vector3(0,1.64f,.015f),new Vector3(.23f,.28f,.22f),skin);
            Box("Hair",root,new Vector3(0,1.77f,.005f),new Vector3(.245f,.065f,.23f),ink);
            Box("Nose",root,new Vector3(0,1.62f,.14f),new Vector3(.055f,.065f,.06f),skin);
            Box("Eyes",root,new Vector3(0,1.68f,.13f),new Vector3(.14f,.025f,.015f),ink);
            if(variant=="B") Box("Vest",root,new Vector3(0,1.2f,.127f),new Vector3(.30f,.42f,.025f),cream);
            else Box("ShirtStripe",root,new Vector3(0,1.21f,.126f),new Vector3(.42f,.055f,.025f),white);
            foreach(var side in new[]{"Left","Right"})
            {
                float sign=side=="Left"?-1:1;
                var arm=Empty(side+"Arm",root,new Vector3(sign*.29f,1.39f,0));
                Box("Sleeve",arm,new Vector3(0,-.13f,0),new Vector3(.15f,.27f,.17f),shirt);
                Box("Forearm",arm,new Vector3(0,-.37f,.015f),new Vector3(.115f,.24f,.12f),skin);
                Box("Hand",arm,new Vector3(0,-.53f,.025f),new Vector3(.115f,.12f,.10f),skin);
                var thigh=Empty(side+"Thigh",root,new Vector3(sign*.115f,.88f,0));
                Box("UpperLeg",thigh,new Vector3(0,-.205f,0),new Vector3(.16f,.41f,.18f),pants);
                var shin=Empty("Shin",thigh,new Vector3(0,-.41f,0));
                Box("LowerLeg",shin,new Vector3(0,-.205f,0),new Vector3(.13f,.41f,.15f),pants);
                var foot=Empty("Foot",shin,new Vector3(0,-.41f,0));
                Box("Shoe",foot,new Vector3(0,-.02f,.055f),new Vector3(.17f,.12f,.29f),ink);
            }
            idle.SampleAnimation(root.gameObject,0);
            PrefabUtility.SaveAsPrefabAsset(root.gameObject,Art+"/HumanVisual_"+variant+".prefab"); UnityEngine.Object.DestroyImmediate(root.gameObject);
        }
    }
    static AnimationClip MakeClip(string name,bool moving)
    {
        var clip=new AnimationClip{name=name,frameRate=30}; float duration=moving?.92f:2f;
        foreach(string side in new[]{"Left","Right"})
        {
            var hip=new List<Keyframe>(); var knee=new List<Keyframe>(); var ankle=new List<Keyframe>(); var arm=new List<Keyframe>();
            for(int i=0;i<=60;i++)
            {
                float time=duration*i/60f, phase=(i/60f+(side=="Left"?0:.5f))%1;
                float z=0,y=.08f;
                if(moving)
                {
                    // Stance: 0.529 m over 0.46 s = 1.15 m/s. Swing clears the floor.
                    if(phase<.5f) z=.2645f-1.058f*phase;
                    else {float u=(phase-.5f)*2;z=-.2645f+.529f*(u*u*(3-2*u));y+=.12f*Mathf.Sin(u*Mathf.PI);}
                }
                float down=.88f-y, distance=Mathf.Sqrt(down*down+z*z);
                float bend=Mathf.Acos(Mathf.Clamp(distance/.82f,-1,1))*Mathf.Rad2Deg;
                float h=-Mathf.Atan2(z,down)*Mathf.Rad2Deg-bend, k=2*bend;
                hip.Add(new Keyframe(time,h)); knee.Add(new Keyframe(time,k)); ankle.Add(new Keyframe(time,-h-k));
                arm.Add(new Keyframe(time,moving?Mathf.Sin(phase*2*Mathf.PI)*19:0));
            }
            Curve(clip,side+"Thigh",hip); Curve(clip,side+"Thigh/Shin",knee); Curve(clip,side+"Thigh/Shin/Foot",ankle); Curve(clip,side+"Arm",arm);
        }
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;settings.loopBlend=true;AnimationUtility.SetAnimationClipSettings(clip,settings);
        string path=Art+"/Animation/"+name+".anim";
        var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if(existing!=null) {EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);return existing;}
        AssetDatabase.CreateAsset(clip,path);return clip;
    }
    static void Curve(AnimationClip clip,string path,List<Keyframe> keys)
    {
        var curve=new AnimationCurve(keys.ToArray());
        for(int i=0;i<curve.length;i++) {AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);}
        clip.SetCurve(path,typeof(Transform),"localEulerAnglesRaw.x",curve);
    }
    static void ConfigureRig()
    {
        var rig=All<XROrigin>().Single(); Empty("CrowdViewerPresence",rig.transform,Vector3.zero);
        foreach(var c in rig.GetComponentsInChildren<MonoBehaviour>(true))
        {
            var so=new SerializedObject(c);
            if(c.GetType().Name=="ControllerInputActionManager")
            {
                // Left thumbstick walks, right retains teleport and snap turn.
                so.FindProperty("m_SmoothMotionEnabled").boolValue=c.name.IndexOf("Left",StringComparison.OrdinalIgnoreCase)>=0;
                so.FindProperty("m_SmoothTurnEnabled").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();
            }
            if(c.GetType().Name.Contains("MoveProvider") && so.FindProperty("m_MoveSpeed")!=null)
            {so.FindProperty("m_MoveSpeed").floatValue=1.15f;so.ApplyModifiedPropertiesWithoutUndo();}
        }
        var marker=All<QuestKeepClearMarker>().Single(); var markerData=new SerializedObject(marker);
        foreach(string field in new[]{"leftRay","rightRay"})
        {
            var ray=(UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor)markerData.FindProperty(field).objectReferenceValue;
            var rayData=new SerializedObject(ray);rayData.FindProperty("m_RaycastMask").intValue|=1<<8;rayData.ApplyModifiedPropertiesWithoutUndo();
        }
    }
    // Safe to run after A lands: strict reflection avoids committing fake A scripts or dangling GUIDs.
    public static void WireMerged()
    {
        EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        Check(WireIfAvailable(),"Merge A's CrowdSceneBindings, CrowdDirector, controls and NetworkOccupant first.");
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene()); AssetDatabase.SaveAssets(); SafeShelter.Editor.IntegrationSetup.Validate();
    }
    static bool WireIfAvailable()
    {
        string[] names={"CrowdSceneBindings","CrowdDirector","CrowdDesktopControls","CrowdViewerPresence"};
        var types=names.Select(n=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("SafeShelter."+n)).FirstOrDefault(t=>t!=null)).ToArray();
        var occupant=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SafeShelter/Prefabs/NetworkOccupant.prefab");
        if(types.Any(t=>t==null)||occupant==null){Debug.Log("DEV B: A runtime absent; named empty objects prepared. Final wiring pending merge.");return false;}
        var root=Find("CrowdDemo").gameObject; if(root.GetComponent<NetworkObject>()==null)root.AddComponent<NetworkObject>();
        var bindings=root.GetComponent(types[0])??root.AddComponent(types[0]); var director=root.GetComponent(types[1])??root.AddComponent(types[1]);
        var b=new SerializedObject(bindings);
        Ref(b,"hallFloor",Find("HallFloor").GetComponent<BoxCollider>());Ref(b,"layout",All<FurnitureLayout>().Single());Ref(b,"blocker",Find("Bed_01_Blocker").GetComponent<SharedFurniture>());
        foreach(string n in new[]{"ClearBedPose","AisleGateSouth","AisleGateNorth","PantryPortal","PantryService","ToiletPortal","ToiletEntry","ToiletService"})Ref(b,char.ToLowerInvariant(n[0])+n.Substring(1),Find(n));
        foreach(var pair in new[]{("residentStarts","ResidentStart_"),("transitQueue","TransitQueue_"),("pantryQueue","PantryQueue_"),("toiletQueue","ToiletQueue_")})
            ArrayRef(b,pair.Item1,Find("Waypoints").Cast<Transform>().Where(t=>t.name.StartsWith(pair.Item2,StringComparison.Ordinal)).OrderBy(t=>t.name,StringComparer.Ordinal).Cast<UnityEngine.Object>().ToArray());
        ArrayRef(b,"staticObstacles",StaticObstacles());
        ArrayRef(b,"occupantVisuals",new UnityEngine.Object[]{AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/HumanVisual_A.prefab"),AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/HumanVisual_B.prefab")});b.ApplyModifiedPropertiesWithoutUndo();
        var d=new SerializedObject(director);Ref(d,"bindings",bindings);Ref(d,"occupantPrefab",occupant);d.ApplyModifiedPropertiesWithoutUndo();
        var desktop=Find("DesktopRig").gameObject;var controls=desktop.GetComponent(types[2])??desktop.AddComponent(types[2]);var c=new SerializedObject(controls);Ref(c,"director",director);c.ApplyModifiedPropertiesWithoutUndo();
        var presence=Find("CrowdViewerPresence").gameObject;var p=new SerializedObject(presence.GetComponent(types[3])??presence.AddComponent(types[3]));Ref(p,"director",director);Ref(p,"viewHead",All<XROrigin>().Single().Camera.transform);p.ApplyModifiedPropertiesWithoutUndo();return true;
    }
    static void Ref(SerializedObject so,string field,UnityEngine.Object value){var p=so.FindProperty(field);Check(p!=null,"Missing A contract field "+field);p.objectReferenceValue=value;}
    static void ArrayRef(SerializedObject so,string field,UnityEngine.Object[] values){var p=so.FindProperty(field);Check(p!=null,"Missing A array "+field);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];}
    static BoxCollider[] StaticObstacles()=>Find("Environment").GetComponentsInChildren<BoxCollider>(true).Where(c=>c.name!="HallFloor" && c.enabled && c.gameObject.activeInHierarchy && !c.isTrigger).OrderBy(c=>c.name,StringComparer.Ordinal).ToArray();
    public static void Validate()
    {
        if(SceneManager.GetActiveScene().path!=ScenePath)EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        SafeShelter.Editor.IntegrationSetup.Validate();Physics.SyncTransforms();
        var anchors=Find("Waypoints").Cast<Transform>().ToArray();Check(anchors.Length==44,"Expected 44 anchors");
        foreach(var t in anchors)Check(t.localScale==Vector3.one,"Anchor scale: "+t.name);
        var floor=Find("HallFloor").GetComponent<BoxCollider>();
        Check(Mathf.Abs(floor.bounds.max.y)<.001f,"Floor top");
        var furniture=All<SharedFurniture>();var solids=StaticObstacles().Concat(furniture.SelectMany(f=>f.GetComponentsInChildren<BoxCollider>())).ToArray();
        foreach(var t in anchors.Where(t=>t.name!="ClearBedPose"))
            foreach(var c in solids)
            {
                var b=c.bounds;b.Expand(new Vector3(.56f,0,.56f));
                Check(!(t.position.x>b.min.x && t.position.x<b.max.x && t.position.z>b.min.z && t.position.z<b.max.z),"Agent clearance: "+t.name+" intersects "+c.name);
            }
        foreach(string name in new[]{"A","B"})
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/HumanVisual_"+name+".prefab");
            Check(prefab.GetComponentsInChildren<Collider>(true).Length==0,"Visual collider");
            Check(prefab.GetComponentsInChildren<MonoBehaviour>(true).Length==0,"Visual runtime script");
            Check(prefab.transform.localScale==Vector3.one,"Visual scale");var animator=prefab.GetComponent<Animator>();
            Check(animator!=null&&!animator.applyRootMotion,"Visual animator/root motion");
            Check(((AnimatorController)animator.runtimeAnimatorController).parameters.Any(p=>p.name=="MoveSpeed"&&p.type==AnimatorControllerParameterType.Float),"MoveSpeed contract");
            var instance=UnityEngine.Object.Instantiate(prefab);var idle=AssetDatabase.LoadAssetAtPath<AnimationClip>(Art+"/Animation/Resident_Idle.anim");idle.SampleAnimation(instance,0);
            var bounds=instance.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
            Check(Mathf.Abs(bounds.min.y)<.002f&&bounds.max.y>=1.65f&&bounds.max.y<=1.85f,"Visual height/grounding "+bounds);
            UnityEngine.Object.DestroyImmediate(instance);
        }
        var report=new List<string>{"DEV B SCENE/ART VALIDATION PASS", "anchors="+anchors.Length,"baseline furniture="+furniture.Length,"static obstacles="+StaticObstacles().Length};
        report.AddRange(anchors.Select(t=>$"ANCHOR {t.name}: {t.position:F3}"));
        report.AddRange(StaticObstacles().Select(c=>"STATIC "+c.name));
        foreach(var c in All<MonoBehaviour>().Where(c=>c!=null&&(c.GetType().Name.Contains("MoveProvider")||c.GetType().Name=="ControllerInputActionManager")))report.Add("RIG "+c.name+" "+EditorJsonUtility.ToJson(c));
        Directory.CreateDirectory("Logs/FinalDemoB");File.WriteAllLines("Logs/FinalDemoB/scene-art-validation.txt",report);
        Debug.Log(string.Join("\n",report.Take(4))); ProbeNavigation();
    }
    static void ProbeNavigation()
    {
        var blocker=Find("Bed_01_Blocker");var original=blocker.position;
        try
        {
            blocker.position=Find("ClearBedPose").position;Physics.SyncTransforms();
            var floor=Find("HallFloor").GetComponent<BoxCollider>();var obstacles=StaticObstacles().Concat(All<SharedFurniture>().SelectMany(f=>f.GetComponentsInChildren<BoxCollider>())).ToArray();
            var sources=new List<NavMeshBuildSource>();
            foreach(var c in new[]{floor}.Concat(obstacles))sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=c.transform.localToWorldMatrix*Matrix4x4.Translate(c.center),size=c.size,area=c==floor?0:1});
            var settings=NavMesh.GetSettingsByIndex(0);settings.agentRadius=.28f;settings.agentHeight=1.75f;settings.agentClimb=.15f;settings.overrideVoxelSize=true;settings.voxelSize=.05f;
            var data=NavMeshBuilder.BuildNavMeshData(settings,sources,new Bounds(Vector3.zero,new Vector3(24,6,16)),Vector3.zero,Quaternion.identity);
            Check(data!=null,"Geometry probe bake failed");var instance=NavMesh.AddNavMeshData(data);
            try
            {
                var anchors=Find("Waypoints").Cast<Transform>().Where(t=>t.name!="ClearBedPose").ToArray();
                foreach(var t in anchors){Check(NavMesh.SamplePosition(t.position,out var hit,.35f,NavMesh.AllAreas),"Nav anchor "+t.name);Check(Mathf.Abs(hit.position.y)<.16f,"Nav height "+t.name);}
                string[] route={"ResidentStart_01","AisleGateSouth","AisleGateNorth","PantryPortal","PantryQueue_08","PantryService","PantryPortal","ToiletPortal","ToiletQueue_04","ToiletEntry","ToiletService"};
                for(int i=1;i<route.Length;i++)
                {
                    NavMesh.SamplePosition(Find(route[i-1]).position,out var a,.35f,NavMesh.AllAreas);NavMesh.SamplePosition(Find(route[i]).position,out var b,.35f,NavMesh.AllAreas);
                    var path=new NavMeshPath();Check(NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"Route "+route[i-1]+" -> "+route[i]);
                }
                Debug.Log("DEV B CLEAR GEOMETRY NAVIGATION PASS: 43 floor anchors, southern approach, gates, pantry and toilets reachable.");
                File.AppendAllText("Logs/FinalDemoB/scene-art-validation.txt","\nCLEAR GEOMETRY NAVIGATION PASS: 43 floor anchors and service route. Not a crowd runtime proof.\n");
            }
            finally{instance.Remove();UnityEngine.Object.DestroyImmediate(data);}
        }
        finally{blocker.position=original;Physics.SyncTransforms();}
    }
    public static void RenderReview()
    {
        EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        var cameraObject=new GameObject("TemporaryReviewCamera"); var camera=cameraObject.AddComponent<Camera>();
        camera.backgroundColor=new Color(.09f,.12f,.16f);camera.clearFlags=CameraClearFlags.SolidColor;camera.nearClipPlane=.03f;camera.farClipPlane=100;
        var extra=cameraObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();extra.renderPostProcessing=false;
        camera.fieldOfView=52;
        Render(camera,new Vector3(0,23,-19),new Vector3(0,0,.5f),"overview");
        Render(camera,new Vector3(-5,3,-.5f),new Vector3(-10,.8f,1),"pantry");
        Render(camera,new Vector3(9.3f,3.3f,7.25f),new Vector3(10.45f,.5f,5.2f),"toilets");
        var a=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/HumanVisual_A.prefab"));a.transform.position=new Vector3(-.65f,0,3);
        var b=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/HumanVisual_B.prefab"));b.transform.position=new Vector3(.65f,0,3);b.transform.rotation=Quaternion.Euler(0,-20,0);
        Render(camera,new Vector3(0,1.25f,6.6f),new Vector3(0,.92f,3),"people-idle");
        var walk=AssetDatabase.LoadAssetAtPath<AnimationClip>(Art+"/Animation/Resident_Walk.anim");walk.SampleAnimation(a,.18f);walk.SampleAnimation(b,.41f);
        Render(camera,new Vector3(0,1.25f,6.6f),new Vector3(0,.92f,3),"people-walk");
        UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);UnityEngine.Object.DestroyImmediate(cameraObject);
    }
    static void Render(Camera camera,Vector3 position,Vector3 target,string name)
    {
        camera.transform.position=position;camera.transform.LookAt(target);
        var rt=new RenderTexture(1600,1000,24);camera.targetTexture=rt;camera.Render();
        var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();Directory.CreateDirectory("Logs/FinalDemoB");File.WriteAllBytes("Logs/FinalDemoB/"+name+".png",image.EncodeToPNG());
        RenderTexture.active=previous;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);
    }
}
