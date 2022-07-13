using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using Photon.Pun;

namespace Kawasaki
{
    /// <summary>
    /// マップの管理者
    /// </summary>
    public class MapsManager : MonoBehaviour
    {
        /// <summary>KillZone のプレハブ名</summary>
        [SerializeField] string _killZonePrefabName;
        /// <summary>KillZone の初期位置</summary>
        [SerializeField] Vector3 _killZoneInitialPosition;

        /// <summary>
        /// 現在のシーンに存在するインスタンス
        /// </summary>
        public static MapsManager Current = null;

        /// <summary>
        /// マップのプレハブ名
        /// </summary>
        [SerializeField]
        string[] _mapPrefabNames = { };

        /// <summary>
        /// マップのグリッド
        /// </summary>
        public Grid MapsGrid { get; set; } = null;

        /// <summary>
        /// 最初に作るマップの数
        /// </summary>
        [SerializeField]
        int _initialMaps = 3;

        /// <summary>
        /// 削除するマップIDのオフセット
        /// </summary>
        [SerializeField]
        int _idOffsetOfMapToRemove = 2;

        /// <summary>
        /// マップの高さ
        /// </summary>
        float _mapsHeight = 0.0f;

        /// <summary>
        /// マップを作った数
        /// </summary>
        public int MapCreationCount { get; private set; } = 0;

        private void Awake()
        {
            Current = this;

            MapsGrid = GetComponentInChildren<Grid>();
        }

        /// <summary>
        /// マップを作る
        /// </summary>
        public void CreateMaps()
        {
            for (int i = 0; i < _initialMaps; i++)
            {
                AddMap();
            }
        }

        /// <summary>
        /// キルゾーンを作る
        /// </summary>
        public void CreateKillZone()
        {
            PhotonNetwork.Instantiate(_killZonePrefabName, _killZoneInitialPosition, Quaternion.identity);
        }

        /// <summary>
        /// マップを更新する
        /// </summary>
        /// <param name="player">プレイヤー</param>
        /// <param name="map">プレイヤーが触れたマップ</param>
        public void UpdateMaps(Player player, Map map)
        {
            // マップが増やせる場合
            if (map.CanAddMap)
            {
                // マップを増やす
                AddMap();

                // マップに再び触れても増やせないようにする
                map.CanAddMap = false;
            }

            // 最下位のプレイヤーが触れた場合
            if (player.IsInTheLowestPosition)
            {
                // マップを削除する
                RemoveMap(map.Id - _idOffsetOfMapToRemove);
            }
        }

        /// <summary>
        /// マップを追加する
        /// </summary>
        public void AddMap()
        {
            // マスタークライアントのみ実行できる
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }

            // 位置
            Vector3 position = new(0.0f, _mapsHeight, 0.0f);

            // プレハブ名
            int index = Random.Range(0, _mapPrefabNames.Length);
            string prefabName = _mapPrefabNames[index];

            // インスタンス
            GameObject instance = PhotonNetwork.Instantiate(prefabName, position, Quaternion.identity);

            // マップのセットアップと同期を行う
            Map map = instance.GetComponent<Map>();
            map.SetUpAndSynchronize(MapCreationCount);

            // マップ作成数を増やす
            MapCreationCount++;

            // マップの高さを更新する
            UpdateMapsHeight(map);
        }

        /// <summary>
        /// マップの高さを更新する
        /// </summary>
        /// <param name="addedMap">追加されたマップ</param>
        private void UpdateMapsHeight(Map addedMap)
        {
            // マスタークライアントのみ実行できる
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }

            // マップが持つ最も高いタイルマップ
            Tilemap tallest = addedMap
                .GetComponentsInChildren<Tilemap>()
                .OrderByDescending(x => x.cellBounds.yMax - x.cellBounds.yMin)
                .FirstOrDefault();

            float height = 0.0f;
            if (tallest)
            {
                height = (tallest.cellBounds.yMax - tallest.cellBounds.yMin) * tallest.cellSize.y;
            }

            // マップの高さにそれを加える
            _mapsHeight += height;
        }

        /// <summary>
        /// マップを削除する
        /// </summary>
        /// <param name="id">削除対象のID</param>
        public void RemoveMap(int id)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }

            Map map = GameObject.FindGameObjectsWithTag("Map")
                .Select(x => x.GetComponent<Map>())
                .FirstOrDefault(x => x.Id == id);

            if (map)
            {
                PhotonNetwork.Destroy(map.gameObject);
            }
        }
    }
}
