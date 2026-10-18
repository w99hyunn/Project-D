using System;
using DoHwa.World;
using UnityEngine;

namespace DoHwa.UI
{
    public sealed class WorldNavigation : MonoBehaviour
    {
        [SerializeField] private WorldRuntime world;
        [SerializeField] private CoopSession session;
        private bool returning;
        public async void ReturnToMain()
        {
            if(returning)return;returning=true;
            try
            {
                if(world.IsAuthority)world.Save();session.Leave();
                await UiSceneTransition.LoadAsync("Main");
            }
            catch(OperationCanceledException){}
            catch(Exception e){returning=false;Debug.LogException(e);}
        }
    }
}
