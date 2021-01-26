using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class BoneWrangler : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject target;
    public string boneRemoveName;
    void Awake()
    {
        // var skinRender = GetComponent<SkinnedMeshRenderer>();
        // foreach(var bone in skinRender.bones){
        //     Debug.Log(bone.name);
        // }
        //WrangleBones();
        //replaceSkin();
        replaceSkinMod(target, this.transform.parent.gameObject);
    }


    /// <summary>
    /// Given an input RoR2 character body (ex. MageBody) and the prefab skin you want to inject,
    /// this function swaps the skin renderers for the input prefab's ones when there is a name collision (ie both have the MageMesh gameobject)
    /// non-collisions result in added objects from the prefabObject.
    /// It just works! c'est la vie
    /// </summary>
    /// <param name="modelObject"></param>
    private static void replaceSkinMod(GameObject modelObject, GameObject prefabObject){
        
        var originalArmature = modelObject.GetComponentsInChildren<Transform>().First(tf => tf.name.ToLower().Contains("armature"));
        var name = originalArmature.name;

        var armature = prefabObject.GetComponentsInChildren<Transform>().First( tf => tf.name.ToLower().Contains("armature"));

        var skinRenderer = modelObject.GetComponentInChildren<SkinnedMeshRenderer>();
        if(skinRenderer == null){
            var obj = new GameObject("MyMesh");
            obj.transform.parent = armature.parent;
            skinRenderer = obj.AddComponent<SkinnedMeshRenderer>();
        }
        var newSkinRenderers = armature.parent.GetComponentsInChildren<SkinnedMeshRenderer>();

        var startTfDict = originalArmature.parent.GetComponentsInChildren<Transform>().ToDictionary(tf => tf.name);
        var conversions = new Dictionary<Transform,Transform>(); //show how to convert new transform tree into original one

        var bones = skinRenderer.bones;
        var nBones = newSkinRenderers[0].bones;

        //disable skinmeshrenderer so it odesnt mess up setup of bones
        skinRenderer.enabled = false;

        //This loop runs on the assumption that parents are always evaluated before their children. If not then uh-oh...
        Transform[] finalBones = new Transform[nBones.Length];
        for(int i=0; i < nBones.Length; i++){
            var bone = nBones[i];
            Transform oldBone;
            if(!startTfDict.TryGetValue(bone.name, out oldBone)){ //if not old bone exists
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
            

            if(oldBone.transform.localRotation != bone.localRotation){
                Debug.Log("Cannot edit rotation of bone: "+oldBone.name);
            }
        }
        Transform myRoot = originalArmature.GetComponentsInChildren<Transform>().First(b => b.name == "ROOT");
        for(int k=0; k < newSkinRenderers.Length; k++){
            var skin = newSkinRenderers[k];
            Transform bone;
            SkinnedMeshRenderer oldSkin;
            if(startTfDict.TryGetValue(skin.name, out bone)){ //if found skin with name 
                oldSkin = bone.GetComponent<SkinnedMeshRenderer>();
                if(oldSkin == null) //no skinned mesh? create one!
                    oldSkin = bone.gameObject.AddComponent<SkinnedMeshRenderer>();
            }else{ //didnt find skin with same name. So create one!
                //meshes are created at same level as the armature
                Transform parent;
                if(!conversions.TryGetValue(skin.transform.parent, out parent)) //allows meshes to be laoded correctly even if not a child of mdl.
                    parent = originalArmature.parent;
                oldSkin = GameObject.Instantiate(skin.gameObject,parent).GetComponent<SkinnedMeshRenderer>();
                oldSkin.name = skin.name;
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
        //Destroy(armature.gameObject);
    }


    // private void replaceSkin()
    // {
    //     var myRenderer = GetComponent<SkinnedMeshRenderer>();
    //     //var otherRoot = replacer.rootBone;
    //     var otherRenderer = replacer;

        
    //     var newArmature = Instantiate(root.transform.parent.gameObject,otherRenderer.rootBone,false);
    //     var children = newArmature.GetComponentsInChildren<Transform>().ToDictionary(tf => tf.name);


    //     //otherRenderer.rootBone = otherRoot.transform;
    //     otherRenderer.sharedMesh = myRenderer.sharedMesh;
    //     //YOU CANT REPLACE BONES, BUT YOU CAN ADD THEM
    //     //throw new NotImplementedException();
    //     otherRenderer.bones = myRenderer.bones.Select(b=>{
    //         return children[b.name];
    //     }).ToArray();
        
    //     otherRenderer.materials = myRenderer.materials;
 

    // }


    private void WrangleBones(Transform root)
    {
        if (string.IsNullOrEmpty(boneRemoveName)) return;
        var skinRender = GetComponent<SkinnedMeshRenderer>();
        var index = 0;
        Transform boneSearch = null;
        // var result = ReadChildren(root, (tf) =>
        // {
        //     if (tf.name.EndsWith("_end")) return false;
        //     //Debug.Log(tf.name);
        //     if (tf.name == boneRemoveName)
        //     {
        //         //Debug.Log("Found @ "+index);
        //         boneSearch = tf;
        //         return true;
        //     }
        //     index++;
        //     return false;
        // });
        var bones = skinRender.bones.ToList();

        List<Mesh> meshes = new List<Mesh>();
        meshes.Add((Mesh)Instantiate(skinRender.sharedMesh));
        DeleteBone(boneSearch, bones, meshes);
        skinRender.bones = bones.ToArray();

        skinRender.sharedMesh = meshes[0];
    }

    //given an input bone to delete and the list of bones, deletes it and modifies the array of boens to correct for that.
    static void DeleteBone(Transform bone, List<Transform> boneList, List<Mesh> mesh){
        //get bone and children to know what we are dleeting
        List<Transform> bonesToDelete = new List<Transform>();
        //get bones we are removing
        ReadChildren(bone.gameObject,b =>{
            if (b.name.EndsWith("_end") || !boneList.Any(bl => bl.name == bone.name)) return; //if end bone or not in bonelist, ignore
            bonesToDelete.Add(b);
            
        });
        bonesToDelete.Add(bone); //add base bone too!

        //with list of bones, make sure all matching names in bone list are removed
        foreach(var b in bonesToDelete){
            var index = boneList.IndexOf(b);
            var parentIndex = boneList.IndexOf(b.parent);
            boneList.RemoveAt(index);
            //Debug.Log("Remove Bone: " + b.name);
            foreach(var m in mesh)
                RemoveBoneIndexFromMesh(index, parentIndex,m);
            
        }
        // //when a bone is deleted, there is no longer a _end gameobject, and that messes up stuff, so we change the bone name to ${parent}_end
        // //this is why we only destroy the children 
        if(bone.parent.childCount == 1)
            bone.name = bone.parent.name + "_end";
        else
            Destroy(bone.gameObject);
        
        //destroy the unneeded bones!
        ReadChildren(bone.gameObject, b =>{
            Destroy(b.gameObject);
            Debug.Log("Destroy: " + b.name);
        });
    
    }
    static public void ReadChildren(GameObject parent, System.Action<Transform> onChild) => ReadChildren(parent, c => {
            onChild(c); return false;
        });
    
    static public bool ReadChildren(GameObject parent, System.Func<Transform,bool> onChild)
    {
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            var child = parent.transform.GetChild(i);
            if (child == null)
                return false;

            string active = child.gameObject.activeInHierarchy.ToString();

            if(onChild(child)){
                return true; //found child, quit
            }
            

            if(ReadChildren(child.gameObject, onChild)) return true; //found child, quit
        }
        return false;
    }
    
    static void RemoveBoneIndexFromMesh(int boneIndex, int parentIndex, Mesh mesh){
        var poses = mesh.bindposes.ToList();
        var weights = mesh.boneWeights;


        poses.RemoveAt(boneIndex);
        for(int i=0; i < weights.Length; i++){
            var w = weights[i];
            if(w.boneIndex0 >= boneIndex){
                w.boneIndex0--;
                //if(w.boneIndex0 == boneIndex) w.boneIndex0 = parentIndex;
            }
            if (w.boneIndex1 >= boneIndex){
                w.boneIndex1--;
                //if (w.boneIndex1 == boneIndex) w.boneIndex1= parentIndex;
            }
            if (w.boneIndex2 >= boneIndex){
                w.boneIndex2--;
                //if (w.boneIndex2 == boneIndex) w.boneIndex2 = parentIndex;
            }
            if (w.boneIndex3 >= boneIndex){
                w.boneIndex3--;
                //if (w.boneIndex3 == boneIndex) w.boneIndex0 = parentIndex;
            }
            weights[i] = w;
        }
        mesh.bindposes = poses.ToArray();
        mesh.boneWeights = weights.ToArray();

        
    }
    
   
}
