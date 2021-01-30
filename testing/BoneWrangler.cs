using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


/// <summary>
/// This script is used to test the correctness of the ReplaceSkinMod functionality without having to import it to RoR2.
/// To use, just add this Script to the object you want to replace, and set target to the prefab you want to import to RoR2.
/// When the game plays, you should see the object this is attached to turn into the prefab. If that does not happen, something has gone wrong!
/// </summary>

public class BoneWrangler : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject target;
    public string boneRemoveName;
    void Awake()
    {
  

        replaceSkinMod(this.gameObject, target);
    }


    /// <summary>
    /// Given an input RoR2 character body (ex. MageBody) and the prefab skin you want to inject,
    /// this function swaps the skin renderers for the input prefab's ones when there is a name collision (ie both have the MageMesh gameobject)
    /// non-collisions result in added objects from the prefabObject.
    /// It just works! c'est la vie
    /// </summary>
    /// <param name="tagetObject"></param>
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
        var index = -1;
        var startTfDict = originalArmature.parent.GetComponentsInChildren<Transform>().ToDictionary(tf => {
            index++;
            return tf.name +"-"+ index;
            });
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
            Transform oldBone;
            if(!startTfDict.TryGetValue(bone.name, out oldBone)){ //if new bone does not exist in original armature
                if(!conversions.TryGetValue(bone.parent,out var parent)){
                    Debug.Log($"Bone List ordered a child bone {bone.name} before it's parent bone {bone.parent.name}");
                }
                //var parent = conversions[bone.parent]; //get the parent of the original bone, and find ITS conversion
                oldBone = new GameObject(bone.name).transform; //create bone in original armature 

                // yes I could have just moved/instantiated the old bone tree instead of making a new bone, but that makes things messy
                oldBone.transform.parent = parent;
            }
            conversions[bone] = oldBone;
            finalBones[i] = oldBone;
            bone.gameObject.CopyAllComponents(oldBone.gameObject);

            if(oldBone.transform.localRotation != bone.localRotation){
                Debug.Log("Cannot edit rotation of bone: "+oldBone.name);
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
            skin.gameObject.CopyAllComponents(bone.gameObject);
            
            bone.GetComponent<SkinnedMeshRenderer>().bones = finalBones;

        }

        skinRenderer.enabled = true;
        Destroy(armature.gameObject);
    }
    
   
}

public static class ComponentExtensions{
  
    public static void CopyAllComponents(this GameObject original, GameObject destination){
        var parent = destination.transform.parent;
        foreach(var comp in original.GetComponents<Component>()){
            comp.CopyComponent(destination);
        }
        destination.transform.parent = parent;
    }
    public static T CopyComponent<T>(this T original, GameObject destination) where T : Component
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