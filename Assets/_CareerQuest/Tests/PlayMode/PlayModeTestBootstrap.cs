using System.Collections;
using CareerQuest;
using UnityEngine;

namespace CareerQuest.Tests
{
    internal static class PlayModeTestBootstrap
    {
        public static IEnumerator EnterPlayCampus(CareerQuestApp app)
        {
            app.ShowAvatarSelectionForPlay();
            yield return null;
            app.ChooseAvatar(AvatarConfig.DefaultAvatarId);
            yield return null;
        }
    }
}
