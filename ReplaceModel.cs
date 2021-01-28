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
        private static PadoruEngiPlugin Instance { get; set; }
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
        private static void replaceSkinMod(GameObject modelObject, GameObject prefabObject)
        {

            var originalArmature = modelObject.GetComponentsInChildren<Transform>().First(tf => tf.name.ToLower().Contains("armature"));
            var name = originalArmature.name;

            var armature = prefabObject.GetComponentsInChildren<Transform>().First(tf => tf.name.ToLower().Contains("armature"));

            var skinRenderer = modelObject.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinRenderer == null)
            {
                var obj = new GameObject("MyMesh");
                obj.transform.parent = armature.parent;
                skinRenderer = obj.AddComponent<SkinnedMeshRenderer>();
            }
            var newSkinRenderers = armature.parent.GetComponentsInChildren<SkinnedMeshRenderer>();

            var startTfDict = originalArmature.parent.GetComponentsInChildren<Transform>().ToDictionary(tf => tf.name);
            var conversions = new Dictionary<Transform, Transform>(); //show how to convert new transform tree into original one

            var bones = skinRenderer.bones;
            var nBones = newSkinRenderers[0].bones;

            //disable skinmeshrenderer so it odesnt mess up setup of bones
            skinRenderer.enabled = false;

            //This loop runs on the assumption that parents are always evaluated before their children. If not then uh-oh...
            Transform[] finalBones = new Transform[nBones.Length];
            for (int i = 0; i < nBones.Length; i++)
            {
                var bone = nBones[i];
                Transform oldBone;
                if (!startTfDict.TryGetValue(bone.name, out oldBone))
                { //if not old bone exists
                    var parent = conversions[bone.parent]; //convert to parent for original tree
                    oldBone = new GameObject(bone.name).transform; //create bone in original armature 

                    // yes I could have just moved/instantiated the old bone tree instead of making a new bone, but that makes things messy
                    oldBone.transform.parent = parent;
                }
                conversions[bone] = oldBone;
                finalBones[i] = oldBone;
                oldBone.transform.localRotation = bone.localRotation;
                oldBone.transform.localPosition = bone.localPosition;
                oldBone.transform.localScale = bone.localScale;


                if (oldBone.transform.localRotation != bone.localRotation)
                {
                    Debug.Log("Cannot edit rotation of bone: " + oldBone.name);
                }
            }
            Transform myRoot = originalArmature.GetComponentsInChildren<Transform>().First(b => b.name == "ROOT");
            for (int k = 0; k < newSkinRenderers.Length; k++)
            {
                var skin = newSkinRenderers[k];
                Transform bone;
                SkinnedMeshRenderer oldSkin;
                if (startTfDict.TryGetValue(skin.name, out bone))
                { //if found skin with name 
                    oldSkin = bone.GetComponent<SkinnedMeshRenderer>();
                    if (oldSkin == null) //no skinned mesh? create one!
                        oldSkin = bone.gameObject.AddComponent<SkinnedMeshRenderer>();
                }
                else
                { //didnt find skin with same name. So create one!
                  //meshes are created at same level as the armature
                    Transform parent;
                    if (!conversions.TryGetValue(skin.transform.parent, out parent)) //allows meshes to be laoded correctly even if not a child of mdl.
                        parent = originalArmature.parent;
                    oldSkin = GameObject.Instantiate(skin.gameObject, parent).GetComponent<SkinnedMeshRenderer>();
                }
                oldSkin.bones = finalBones; //bones 
                oldSkin.materials = skin.materials;
                oldSkin.sharedMesh = skin.sharedMesh;
                oldSkin.rootBone = myRoot; //needs to be set for new skinmesh renderers
            }
            // var newSkin = armature.parent.GetComponentsInChildren<SkinnedMeshRenderer>().FirstOrDefault(c => c.name == "EngiMesh");
            // skinRenderer.bones = finalBones;
            // skinRenderer.materials = newSkin.materials;
            // skinRenderer.sharedMesh = newSkin.sharedMesh;

            skinRenderer.enabled = true;
            Destroy(armature.gameObject);
        }
    }
}