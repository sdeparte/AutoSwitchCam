using AutoSwitchCam.Model;
using AutoSwitchCam.Properties;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace AutoSwitchCam.Services
{
    public class OBSLinker
    {
        public event EventHandler<ObservableCollection<ObservableScene>> OBSLinker_Connected;

        private OBSWebsocket _obs;

        public OBSLinker()
        {
            _obs = new OBSWebsocket();
            _obs.Connected += WebSocket_Connected;
        }

        public void Connect()
        {
            try
            {
                _obs.Connect(Resources.ObsUri, Resources.ObsPassword);
            }
            catch (AuthFailureException)
            {
                MessageBox.Show("Authentication failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            catch (ErrorResponseException ex)
            {
                MessageBox.Show("Connect failed : " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        private void WebSocket_Connected(object sender, EventArgs e)
        {
            ObservableCollection<ObservableScene> listScenes = new ObservableCollection<ObservableScene>();

            foreach (OBSScene obsScene in _obs.ListScenes())
            {
                listScenes.Add(new ObservableScene { OBSScene = obsScene });
            }

            OBSLinker_Connected?.Invoke(this, listScenes);
        }

        public ObservableCollection<ObservableSceneItem> LoadOBSSceneItems(ObservableScene scene)
        {
            ObservableCollection<ObservableSceneItem> listSceneItems = new ObservableCollection<ObservableSceneItem>();

            foreach (SceneItem sceneItem in scene.OBSScene.Items)
            {
                listSceneItems.Add(new ObservableSceneItem { SceneItem = sceneItem });
            }

            return listSceneItems;
        }

        public void SetSceneVisibility(string sceneName, string sceneItemName, bool visibility)
        {
            _obs.SetSourceRender(sceneItemName, visibility, sceneName);
        }
    }
}
