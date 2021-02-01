using BepInEx.Logging;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

///
/// Huge Thanks to 
/// KingEnderBrine for his wonderful tutorials.
/// RuneFox237 for his Dynamic Bone addition script that I used as a reference.
/// 

//NameSpace and SkinName are generated from SkinDef Generator
namespace %%PluginName%%
{
    public partial class %%PluginName%%Plugin
    {

        ///////////////////////////////////////////////////////////
        /// Stuff you should Change
        const string New_MODEL_ASSET_NAME = "%%PREFABNAME%%.prefab";
        private static %%PluginName%%Plugin Instance { get; set; }

        partial void AfterAwake()
        {
            //TODO: Add Add modification data you want here.
            //////////////////////////////////////////////////

        }

        //Name for this is generated from the skinDef generator
        static partial void %%SkinName%%SkinAdded(SkinDef skinDef, GameObject bodyPrefab)
        {
            SkinDef = skinDef;
        }


////// DO NOT GO PAST THIS POINT /////// DO NOT GO PAST THIS POINT /////// DO NOT GO PAST THIS POINT /////// DO NOT GO PAST THIS POINT /////// 

        //This uses Name of class 
        public static SkinDef SkinDef { get; private set; }
        
        private static ManualLogSource InstanceLogger => Instance?.Logger;
        private static bool? MyModification = null;

        /// Local Declarations
        ///////////////////////////////////////////////////////////


        partial void BeforeAwake()
        {
            Instance = this;

            On.RoR2.SkinDef.Apply += MySkinApply;
        }

        
        ////////////////////////////////////////////////////////////////////////////
        ////// Local Functions (these should not need to be changed when added to different skins)

        private static void MySkinApply(On.RoR2.SkinDef.orig_Apply orig, SkinDef self, GameObject modelObject){
            orig(self, modelObject);
            try{
                //if another character
                if(self != SkinDef){
                    return; //TODO: clear change
                }
                else if(MyModification == null){ //need to modify this skin!
                    var newModelName = New_MODEL_ASSET_NAME;
                    var newModelAsset = assetBundle.LoadAsset(newModelName); //load in new armature to replace old armature
                    if(newModelAsset == null){
                        InstanceLogger.LogError("Unable to load Prefab "+newModelName);
                    }
                    var newModel = GameObject.Instantiate(newModelAsset) as GameObject; //instantiate in world area. Doesnt matter because we delete it later.
                    replaceSkinMod(modelObject, newModel);
                }

            }catch (Exception e)
            {
                //error logging may need to be skin specific
                InstanceLogger.LogWarning("An error occured while adding accessories to a skin");
                InstanceLogger.LogError(e);
            }
        }




        /// <summary>
        /// Given an input RoR2 character body (ex. MageBody) and the prefab skin you want to inject,
        /// this function swaps the skin renderers for the input prefab's ones when there is a name collision (ie both have the MageMesh gameobject)
        /// non-collisions result in added objects from the prefabObject.
        /// It just works! c'est la vie
        /// </summary>
        /// <param name="modelObject"></param>
        private static void replaceSkinMod(GameObject tagetObject, GameObject referenceObject){
        
            var originalArmature = tagetObject.GetComponentsInChildren<Transform>().First(tf => tf.name == "ROOT").parent;
            var name = originalArmature.name;

            var armature = referenceObject.GetComponentsInChildren<Transform>().First( tf => tf.name == "ROOT").parent;

            var skinRenderer = tagetObject.GetComponentInChildren<SkinnedMeshRenderer>();
            if(skinRenderer == null){
                var obj = new GameObject("MyMesh");
                obj.transform.parent = armature.parent;
                skinRenderer = obj.AddComponent<SkinnedMeshRenderer>();
            }
            var newSkinRenderers = referenceObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            var startTfDict = new Dictionary<string,Transform>();
            foreach(var tf in originalArmature.parent.GetComponentsInChildren<Transform>()){
                if(!startTfDict.ContainsKey(tf.name))
                    startTfDict.Add(tf.name,tf);
            }
            var conversions = new Dictionary<Transform,Transform>(); //show how to convert new transform tree into original one

            var bones = skinRenderer.bones;
            var nBones = newSkinRenderers[0].bones;

            //disable skinmeshrenderer so it odesnt mess up setup of bones
            skinRenderer.enabled = false;
            Transform myRoot = originalArmature.GetComponentsInChildren<Transform>().First(b => b.name == "ROOT");
            //This loop runs on the assumption that parents are always evaluated before their children. If not then uh-oh...
            Transform[] finalBones = new Transform[nBones.Length];
            for(int i=0; i < nBones.Length; i++){
                
                var bone = nBones[i];
                var removeIndex = bone.name.IndexOf(" 1"); //remove any name errors because of importing, just in case the user hasn't
                if(removeIndex != -1){
                    bone.name = bone.name.Substring(0,removeIndex);
                }
                Transform oldBone;
                if(!startTfDict.TryGetValue(bone.name, out oldBone)){ //if new bone does not exist in original armature
                    if(!conversions.TryGetValue(bone.parent,out var parent)){
                        InstanceLogger.LogError($"Bone List ordered a child bone {bone.name} before it's parent bone {bone.parent.name}");
                    }
                    //var parent = conversions[bone.parent]; //get the parent of the original bone, and find ITS conversion
                    oldBone = new GameObject(bone.name).transform; //create bone in original armature 

                    // yes I could have just moved/instantiated the old bone tree instead of making a new bone, but that makes things messy
                    oldBone.transform.parent = parent;
                }
                conversions[bone] = oldBone;
                finalBones[i] = oldBone;
                bone.gameObject.CopyAllComponentsTo(oldBone.gameObject); //copy transform, etc

                var dym = oldBone.GetComponent<DynamicBone>(); //The dynamic bones don't work unless you retarget their ROOT to the correct armature
                if(dym != null){
                    if(dym.m_Root != null && conversions.TryGetValue(dym.m_Root,out var newRoot)){
                        dym.m_Root = newRoot;
                    }else{
                        dym.m_Root = dym.transform.parent;
                    }
                }

                if(oldBone.transform.localRotation != bone.localRotation){
                    InstanceLogger.LogWarning("Cannot edit rotation of bone: "+oldBone.name);
                }
            }
            
            for(int k=0; k < newSkinRenderers.Length; k++){
                var skin = newSkinRenderers[k];
                Transform bone;
                if(!startTfDict.TryGetValue(skin.name, out bone)){ //if found gameobject
                    //meshes are created at same level as the armature
                    Transform parent;
                    if(!conversions.TryGetValue(skin.transform.parent, out parent)) //allows meshes to be laoded correctly even if not a child of mdl.
                        parent = originalArmature.parent;
                    bone = new GameObject(skin.name).transform;
                    bone.parent = parent;
                    // oldSkin = GameObject.Instantiate(skin.gameObject,parent).GetComponent<SkinnedMeshRenderer>();
                    // oldSkin.name = skin.name;
                }
                skin.gameObject.CopyAllComponentsTo(bone.gameObject);
                
                bone.GetComponent<SkinnedMeshRenderer>().bones = finalBones;

            }

            skinRenderer.enabled = true;
            Destroy(armature.gameObject);
        }
    }
    public static class ComopnentExtensions{
  
        public static void CopyAllComponentsTo(this GameObject original, GameObject destination){
            var parent = destination.transform.parent;
            var name = destination.name;
            foreach(var comp in original.GetComponents<Component>()){
                comp.CopyComponentTo(destination);
            }
            destination.transform.parent = parent;
            destination.name = name;
        }
        public static T CopyComponentTo<T>(this T original, GameObject destination) where T : Component
        {
            System.Type type = original.GetType();
            var dst = destination.GetComponent(type) as T;
            if (!dst) dst = destination.AddComponent(type) as T;
            var fields = type.GetFields();
            foreach (var field in fields)
            {
                if (field.IsStatic) continue;
                field.SetValue(dst, field.GetValue(original));
            }
            var props = type.GetProperties();
            foreach (var prop in props)
            {
                if (!prop.CanWrite || !prop.CanWrite || prop.Name == "name") continue;
                prop.SetValue(dst, prop.GetValue(original, null), null);
            }
            return dst as T;
        }
    
    }
}