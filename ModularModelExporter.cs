
using PsychoticLab;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public enum BodyPartEnum
{

    Shoulder_Attachment_Right,
    Shoulder_Attachment_Left,
    Elbow_Attachment_Right,
    Knee_Attachement_Right,
    Elbow_Attachment_Left,
    Knee_Attachement_Left,
    Hips_Attachment,
    Arm_Upper_Right,
    Arm_Lower_Right,
    Head_Attachment,
    Back_Attachment,
    Arm_Upper_Left,
    Arm_Lower_Left,
    HeadCovering,
    FacialHair,
    Hand_Right,
    Hand_Left,
    Leg_Right,
    Leg_Left,
    Eyebrows,
    Torso,
    Extra,
    Head,
    Hips,
    Hair,
}

public class ModularModelExporter : MonoBehaviour
{
    public Material mat;

    public string filename = "Model";
    public string directory = "GeneratedModels";
    private int increment;
    private Transform skeletonRoot;

    private Dictionary<BodyPartEnum, GameObject> activeParts;
    private CharacterRandomizer cRan;

    private Array bodyparts;

    private void Start()
    {
        cRan = GetComponent<CharacterRandomizer>();
        bodyparts = Enum.GetValues(typeof(BodyPartEnum));
        
        skeletonRoot = transform.Find("Root");

        activeParts = new Dictionary<BodyPartEnum, GameObject>(bodyparts.Length);
        foreach (BodyPartEnum partType in bodyparts)
            activeParts[partType] = null;
    }

    public void SetIncrement()
    {
        increment = 0;
        if (Directory.Exists($"Assets/{directory}/Prefabs/"))
        {
            var files = Directory.GetFiles($"Assets/{directory}/Prefabs/");
            foreach (string file in files)
            {
                if (file.Contains(".meta")) continue;
                if (ShortToFileName(file, "_") == filename) increment++;
            }
        }
        Debug.Log(increment);
    }

    public string ShortToFileName(string input, string start)
    {
        input = Path.GetFileName(input);
        int first = input.LastIndexOf(start) + start.Length;
        return input.Remove(first - start.Length, input.Length - first + start.Length);
       
    }

    [Button]
    public void Randomizer()
    {
        typeof(CharacterRandomizer).GetMethod("Randomize", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(cRan, null);
    }

    [Button]
    public void Save()
    {
        SetActiveParts();
        SaveDollPrefab();
    }

    public void SetActiveParts()
    {
        foreach (BodyPartEnum partType in bodyparts)
            activeParts[partType] = null;

        List<GameObject> gameObjects = GetComponentsInChildren<SkinnedMeshRenderer>().Where(s => s.gameObject.activeSelf).Select(s => s.gameObject).ToList();

        foreach (GameObject go in gameObjects)
            for (int i = 0; i < activeParts.Count; i++)
                if (go.transform.parent.name.Contains(((BodyPartEnum)i).ToString()))
                {
                    activeParts[(BodyPartEnum)i] = go;
                    break;
                }
    }

    private void SaveDollPrefab()
    {
        SetIncrement();
        var prefabFile = $"Assets/{directory}/Prefabs/{filename}_{increment}.prefab";
        EnsureDir(prefabFile);

        var prefab = new GameObject("Modelholder");
        // Save materials to prefab
        var materialDir = $"Assets/{directory}/Materials";
        EnsureDir($"{materialDir}/{filename}.mat");

        Material _mat = new Material(mat);

        var partName = "Material";
        var materialPath = $"{materialDir}/{filename}_{partName}_{increment}.mat";
        AssetDatabase.CreateAsset(_mat, materialPath);

        Optimize(prefab.transform, _mat);
        PrefabUtility.SaveAsPrefabAsset(prefab, prefabFile);

        // remove the new prefab in the scene
        DestroyImmediate(prefab);
    }

    private void EnsureDir(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    public void Optimize(Transform newRoot, Material _mat)
    {
        var newParts = new GameObject();
        newParts.transform.parent = newRoot.transform;
        newParts.name = "Parts";
        var newSkeleton = GameObject.Instantiate(skeletonRoot, newRoot.transform);
        newSkeleton.name = "Root";

        var reBoner = new ReBoner(newSkeleton);
        foreach (BodyPartEnum partType in bodyparts)
        {
            var srcPart = activeParts[partType];
            if (srcPart != null)
            {
                var newPart = GameObject.Instantiate(srcPart, newParts.transform);
                var newSkin = newPart.GetComponent<SkinnedMeshRenderer>();
                newSkin.material = _mat;
                reBoner.ReBone(newSkin);
            }
        }
    }
}

public class ReBoner
{
    private Dictionary<string, Transform> boneMap;

    public ReBoner(Transform skeleton)
    {
        boneMap = new Dictionary<string, Transform>();
        BFSTransform(skeleton, tf => { boneMap[tf.gameObject.name] = tf; });
    }

    public void ReBone(SkinnedMeshRenderer skin)
    {
        var boneArray = skin.bones;
        for (int idx = 0; idx < boneArray.Length; ++idx)
        {
            string boneName = boneArray[idx].name;
            boneArray[idx] = boneMap[boneName];
        }

        skin.bones = boneArray;
        skin.rootBone = boneMap[skin.rootBone.name];
    }

    public static void BFSTransform(Transform root, Action<Transform> process)
    {
        var q = new Queue<Transform>();
        var visited = new HashSet<int>();
        q.Enqueue(root);

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            var id = cur.GetInstanceID();
            if (!visited.Contains(id))
            {
                process(cur);
                foreach (Transform child in cur.transform)
                {
                    q.Enqueue(child);
                }
                visited.Add(id);
            }
        }
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class ButtonAttribute : Attribute
{
    public ButtonAttribute(string label) => Label = label;

    public ButtonAttribute()
    {
    }

    public string Label { get; }
}

[CustomEditor(typeof(UnityEngine.Object), true)]
public class ButtonAttributeInspectors : Editor
{
    MethodInfo[] Methods => target.GetType()
        .GetMethods(BindingFlags.Instance |
                    BindingFlags.Static |
                    BindingFlags.NonPublic |
                    BindingFlags.Public);

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        ButtonMethods();
    }

    void ButtonMethods()
    {
        if (Methods.Length < 1)
            return;

        foreach (var method in Methods)
        {
            var buttonAttribute = (ButtonAttribute)method
                .GetCustomAttribute(typeof(ButtonAttribute));

            if (buttonAttribute != null)
                Button(buttonAttribute, method);
        }
    }

    public void Button(ButtonAttribute buttonAttribute, MethodInfo method)
    {
        var label = buttonAttribute.Label ?? method.Name;

        if (GUILayout.Button(label))
            method.Invoke(target, null);
    }
}