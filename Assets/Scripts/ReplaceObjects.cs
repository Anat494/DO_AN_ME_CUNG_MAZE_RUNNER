using UnityEngine;
using UnityEditor;

public class ReplaceObjects : EditorWindow
{
    public GameObject newPrefab;
    public Transform parentObject;

    [MenuItem("Tools/Replace Objects")]
    public static void ShowWindow()
    {
        GetWindow<ReplaceObjects>("Replace Objects");
    }

    private void OnGUI()
    {
        newPrefab = (GameObject)EditorGUILayout.ObjectField("New Prefab", newPrefab, typeof(GameObject), false);
        parentObject = (Transform)EditorGUILayout.ObjectField("Parent Group (Optional)", parentObject, typeof(Transform), true);

        if (GUILayout.Button("Replace Selected Objects"))
        {
            if (newPrefab == null) return;

            // Đăng ký nhóm đối tượng để có thể Ctrl + Z lại nếu muốn
            Undo.RegisterCreatedObjectUndo(newPrefab, "Replace Objects");

            foreach (GameObject selectedObj in Selection.gameObjects)
            {
                Vector3 pos = selectedObj.transform.position;
                Quaternion rot = selectedObj.transform.rotation;
                Vector3 scale = selectedObj.transform.localScale;

                GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(newPrefab);
                newObj.transform.position = pos;
                newObj.transform.rotation = rot;
                newObj.transform.localScale = scale;

                if (parentObject != null)
                {
                    newObj.transform.SetParent(parentObject);
                }
                else
                {
                    // Đưa đối tượng mới vào cùng cấp với đối tượng cũ
                    newObj.transform.SetParent(selectedObj.transform.parent);
                }

                // Ghi nhận lệnh Undo cho việc xóa object cũ
                Undo.DestroyObjectImmediate(selectedObj);
            }
        }
    }
}