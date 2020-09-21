
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
    }

    public string ShortToFileName(string input, string start)
    {
        input = Path.GetFileName(input);
        int first = input.LastIndexOf(start) + start.Length;
        return input.Remove(first - start.Length, input.Length - first + start.Length);
       
    }


#if (UNITY_EDITOR)
    public void InitBody()
    {
        cRan = GetComponent<CharacterRandomizer>();

        // rebuild all lists
        typeof(CharacterRandomizer).GetMethod("BuildLists", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(cRan, null);
        
        // disable any enabled objects before clear
        if (cRan.enabledObjects.Count != 0)
        {
            foreach (GameObject g in cRan.enabledObjects)
            {
                g.SetActive(false);
            }
        }

        // clear enabled objects list
        cRan.enabledObjects.Clear();
    }

    // method for rolling percentages (returns true/false)
    bool GetPercent(int pct)
    {
        bool p = false;
        int roll = UnityEngine.Random.Range(0, 100);
        if (roll <= pct)
        {
            p = true;
        }
        return p;
    }

    public void InitColor()
    {
        cRan = GetComponent<CharacterRandomizer>();
    }

    public SkinColor GetSkinColor()
    {
        Color mySkin = mat.GetColor("_Color_Skin");
        Race myRace = Race.Elf;
        foreach (Color c in cRan.whiteSkin)
            if (mySkin == c) myRace = Race.Human;

        foreach (Color c in cRan.blackSkin)
            if (mySkin == c) myRace = Race.Human;
        
        foreach (Color c in cRan.brownSkin)
            if (mySkin == c) myRace = Race.Human;

        switch (myRace)
        {
            case Race.Human:
                // select human skin 33% chance for each
                int colorRoll = UnityEngine.Random.Range(0, 100);
                // select white skin
                if (colorRoll <= 33)
                    return SkinColor.White;
                // select brown skin
                if (colorRoll > 33 && colorRoll < 66)
                    return SkinColor.Brown;
                // select black skin
                return SkinColor.Black;
            case Race.Elf:
                // select elf skin
                return SkinColor.Elf;
            default:
                return SkinColor.Brown;
        }
    }

    public void InitExport()
    {
        bodyparts = Enum.GetValues(typeof(BodyPartEnum));

        skeletonRoot = transform.Find("Root");

        activeParts = new Dictionary<BodyPartEnum, GameObject>(bodyparts.Length);
        foreach (BodyPartEnum partType in bodyparts)
            activeParts[partType] = null;
    }
#endif

    [Button]
    public void RandomizeBody()
    {
#if (UNITY_EDITOR)
        InitBody();
#endif

        // initialize settings
        Gender gender = Gender.Male;
        Race race = Race.Human;
        SkinColor skinColor = SkinColor.White;
        Elements elements = Elements.Yes;
        HeadCovering headCovering = HeadCovering.HeadCoverings_Base_Hair;
        FacialHair facialHair = FacialHair.Yes;

        // roll for gender
        if (!GetPercent(50))
            gender = Gender.Female;

        // roll for human (70% chance, 30% chance for elf)
        if (!GetPercent(70))
            race = Race.Elf;

        // roll for facial elements (beard, eyebrows)
        if (!GetPercent(50))
            elements = Elements.No;

        // select head covering 33% chance for each
        int headCoveringRoll = UnityEngine.Random.Range(0, 100);
        // HeadCoverings_Base_Hair
        if (headCoveringRoll <= 33)
            headCovering = HeadCovering.HeadCoverings_Base_Hair;
        // HeadCoverings_No_FacialHair
        if (headCoveringRoll > 33 && headCoveringRoll < 66)
            headCovering = HeadCovering.HeadCoverings_No_FacialHair;
        // HeadCoverings_No_Hair
        if (headCoveringRoll >= 66)
            headCovering = HeadCovering.HeadCoverings_No_Hair;

        // select skin color if human, otherwise set skin color to elf
        switch (race)
        {
            case Race.Human:
                // select human skin 33% chance for each
                int colorRoll = UnityEngine.Random.Range(0, 100);
                // select white skin
                if (colorRoll <= 33)
                    skinColor = SkinColor.White;
                // select brown skin
                if (colorRoll > 33 && colorRoll < 66)
                    skinColor = SkinColor.Brown;
                // select black skin
                if (colorRoll >= 66)
                    skinColor = SkinColor.Black;
                break;
            case Race.Elf:
                // select elf skin
                skinColor = SkinColor.Elf;
                break;
        }

        CharacterObjectGroups cog = cRan.female;
        CharacterObjectListsAllGender allGender = cRan.allGender;
        //roll for gender
        switch (gender)
        {
            case Gender.Male:
                cog = cRan.male;
                // roll for facial hair if male
                if (!GetPercent(50))
                    facialHair = FacialHair.No;
                break;

            case Gender.Female:
                cog = cRan.female;
                // no facial hair if female
                facialHair = FacialHair.No;

                break;
        }

        // if facial elements are enabled
        switch (elements)
        {
            case Elements.Yes:
                //select head with all elements
                if (cog.headAllElements.Count != 0)
                    ActivateItem(cog.headAllElements[UnityEngine.Random.Range(0, cog.headAllElements.Count)]);

                //select eyebrows
                if (cog.eyebrow.Count != 0)
                    ActivateItem(cog.eyebrow[UnityEngine.Random.Range(0, cog.eyebrow.Count)]);

                //select facial hair (conditional)
                if (cog.facialHair.Count != 0 && facialHair == FacialHair.Yes && gender == Gender.Male && headCovering != HeadCovering.HeadCoverings_No_FacialHair)
                    ActivateItem(cog.facialHair[UnityEngine.Random.Range(0, cog.facialHair.Count)]);

                // select hair attachment
                switch (headCovering)
                {
                    case HeadCovering.HeadCoverings_Base_Hair:
                        // set hair attachment to index 1
                        if (allGender.all_Hair.Count != 0)
                            ActivateItem(allGender.all_Hair[1]);
                        if (allGender.headCoverings_Base_Hair.Count != 0)
                            ActivateItem(allGender.headCoverings_Base_Hair[UnityEngine.Random.Range(0, allGender.headCoverings_Base_Hair.Count)]);
                        break;
                    case HeadCovering.HeadCoverings_No_FacialHair:
                        // no facial hair attachment
                        if (allGender.all_Hair.Count != 0)
                            ActivateItem(allGender.all_Hair[UnityEngine.Random.Range(0, allGender.all_Hair.Count)]);
                        if (allGender.headCoverings_No_FacialHair.Count != 0)
                            ActivateItem(allGender.headCoverings_No_FacialHair[UnityEngine.Random.Range(0, allGender.headCoverings_No_FacialHair.Count)]);
                        break;
                    case HeadCovering.HeadCoverings_No_Hair:
                        // select hair attachment
                        if (allGender.headCoverings_No_Hair.Count != 0)
                            ActivateItem(allGender.all_Hair[UnityEngine.Random.Range(0, allGender.all_Hair.Count)]);
                        // if not human
                        if (race != Race.Human)
                        {
                            // select elf ear attachment
                            if (allGender.elf_Ear.Count != 0)
                                ActivateItem(allGender.elf_Ear[UnityEngine.Random.Range(0, allGender.elf_Ear.Count)]);
                        }
                        break;
                }
                break;

            case Elements.No:
                //select head with no elements
                if (cog.headNoElements.Count != 0)
                    ActivateItem(cog.headNoElements[UnityEngine.Random.Range(0, cog.headNoElements.Count)]);
                break;
        }

        // select torso starting at index 1
        if (cog.torso.Count != 0)
            ActivateItem(cog.torso[UnityEngine.Random.Range(1, cog.torso.Count)]);

        // determine chance for upper arms to be different and activate
        if (cog.arm_Upper_Right.Count != 0)
            RandomizeLeftRight(cog.arm_Upper_Right, cog.arm_Upper_Left, 15);

        // determine chance for lower arms to be different and activate
        if (cog.arm_Lower_Right.Count != 0)
            RandomizeLeftRight(cog.arm_Lower_Right, cog.arm_Lower_Left, 15);

        // determine chance for hands to be different and activate
        if (cog.hand_Right.Count != 0)
            RandomizeLeftRight(cog.hand_Right, cog.hand_Left, 15);

        // select hips starting at index 1
        if (cog.hips.Count != 0)
            ActivateItem(cog.hips[UnityEngine.Random.Range(1, cog.hips.Count)]);

        // determine chance for legs to be different and activate
        if (cog.leg_Right.Count != 0)
            RandomizeLeftRight(cog.leg_Right, cog.leg_Left, 15);

        // select chest attachment
        if (allGender.chest_Attachment.Count != 0)
            ActivateItem(allGender.chest_Attachment[UnityEngine.Random.Range(0, allGender.chest_Attachment.Count)]);

        // select back attachment
        if (allGender.back_Attachment.Count != 0)
            ActivateItem(allGender.back_Attachment[UnityEngine.Random.Range(0, allGender.back_Attachment.Count)]);

        // determine chance for shoulder attachments to be different and activate
        if (allGender.shoulder_Attachment_Right.Count != 0)
            RandomizeLeftRight(allGender.shoulder_Attachment_Right, allGender.shoulder_Attachment_Left, 10);

        // determine chance for elbow attachments to be different and activate
        if (allGender.elbow_Attachment_Right.Count != 0)
            RandomizeLeftRight(allGender.elbow_Attachment_Right, allGender.elbow_Attachment_Left, 10);

        // select hip attachment
        if (allGender.hips_Attachment.Count != 0)
            ActivateItem(allGender.hips_Attachment[UnityEngine.Random.Range(0, allGender.hips_Attachment.Count)]);

        // determine chance for knee attachments to be different and activate
        if (allGender.knee_Attachement_Right.Count != 0)
            RandomizeLeftRight(allGender.knee_Attachement_Right, allGender.knee_Attachement_Left, 10);

    }
    // method for handling the chance of left/right items to be differnt (such as shoulders, hands, legs, arms)
    void RandomizeLeftRight(List<GameObject> objectListRight, List<GameObject> objectListLeft, int rndPercent)
    {
        // rndPercent = chance for left item to be different

        // stored right index
        int index = UnityEngine.Random.Range(0, objectListRight.Count);

        // enable item from list using index
        ActivateItem(objectListRight[index]);

        // roll for left item mismatch, if true randomize index based on left item list
        if (GetPercent(rndPercent))
            index = UnityEngine.Random.Range(0, objectListLeft.Count);

        // enable left item from list using index
        ActivateItem(objectListLeft[index]);
    }

    // enable game object and add it to the enabled objects list
    void ActivateItem(GameObject go)
    {
        // enable item
        go.SetActive(true);

        // add item to the enabled items list
        cRan.enabledObjects.Add(go);
    }


    [Button]
    public void RandomizeColor()
    {
#if (UNITY_EDITOR)
        InitColor();
#endif
        System.Object[] skinColor = new System.Object[1];
        skinColor[0] = GetSkinColor();
        typeof(CharacterRandomizer).GetMethod("RandomizeColors", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(cRan, skinColor);
    }

    [Button]
    public void Export()
    {
#if (UNITY_EDITOR)
        InitExport();
#endif
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


        SaveTexture(_mat.GetTexture("_Texture"));

        var partName = "Material";
        var materialPath = $"{materialDir}/{filename}_{partName}_{increment}.mat";
        AssetDatabase.CreateAsset(_mat, materialPath);

        Optimize(prefab.transform, _mat);

        Component[] components = GetComponents(typeof(Component));
        foreach(Component component in components)
        {
            if (component == cRan || component == this || component == transform) continue; 
            CopyComponent(component, prefab);
        }
        PrefabUtility.SaveAsPrefabAsset(prefab, prefabFile);

        // remove the new prefab in the scene      
        DestroyImmediate(prefab);
    }

    public void SaveTexture(Texture mainTexture)
    {
        Texture2D texture2D = new Texture2D(mainTexture.width, mainTexture.height, TextureFormat.RGBA32, false);

        RenderTexture currentRT = RenderTexture.active;
        
        RenderTexture renderTexture = new RenderTexture(mainTexture.width, mainTexture.height, 32);
        Graphics.Blit(null, renderTexture, new Material(mat));

        RenderTexture.active = renderTexture;
        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture2D.Apply();

        RenderTexture.active = null;
        renderTexture.Release();

        var texturelDir = $"Assets/{directory}/Texture";
        var partName = "Texture";
        EnsureDir($"{texturelDir}/{filename}_{partName}_{increment}.png");
        SaveTextureAsPNG(texture2D, $"Assets/GeneratedModels/Texture/{filename}_{partName}_{increment}.png");
    }
    public void SaveTextureAsPNG(Texture2D _texture, string _fullPath)
    {
        byte[] _bytes = _texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(_fullPath, _bytes);
        AssetDatabase.Refresh();
    }

    T CopyComponent<T>(T original, GameObject destination) where T : Component
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
            if (!prop.CanWrite || !prop.CanWrite || prop.Name == "name" || prop.Name == "bodyPosition" || prop.Name == "bodyRotation" || prop.Name == "playbackTime") continue;
            prop.SetValue(dst, prop.GetValue(original, null), null);
        }
        return dst as T;
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