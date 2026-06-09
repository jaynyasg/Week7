using UnityEngine;

namespace CareerQuest
{
    public class CampusGuideController : MonoBehaviour
    {
        public void Configure(string prompt)
        {
            var view = gameObject.GetComponent<AvatarRuntimeView>() ?? gameObject.AddComponent<AvatarRuntimeView>();
            view.ApplyAvatar("art_inventor");

            var labelObject = new GameObject("GuidePrompt", typeof(TextMesh));
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.95f, 0f);

            var label = labelObject.GetComponent<TextMesh>();
            label.text = prompt;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.075f;
            label.fontSize = 64;
            label.color = new Color(0.05f, 0.09f, 0.11f);

            var renderer = labelObject.GetComponent<MeshRenderer>();
            renderer.sortingOrder = 25;
        }
    }
}
