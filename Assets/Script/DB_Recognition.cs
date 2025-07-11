
using System;
using System.Collections.Generic;
using UnityEngine;


namespace DB
{

    [CreateAssetMenu(fileName = "DB_Recognition", menuName = "Scriptable Objects/DB_Recognition")]
    public class DB_Recognition : ScriptableObject
    {

        #region QOL向上処理
        // CakeParamsSOが保存してある場所のパス
        public const string PATH = "DB_Recognition";

        // CakeParamsDBの実体
        private static DB_Recognition _entity = null;
        public static DB_Recognition Entity
        {
            get
            {
                // 初アクセス時にロードする
                if (_entity == null)
                {
                    _entity = Resources.Load<DB_Recognition>(PATH);

                    //ロード出来なかった場合はエラーログを表示
                    if (_entity == null)
                    {
                        Debug.LogError(PATH + " not found");
                    }
                }

                return _entity;
            }
        }
        #endregion

        [Header("DB_Recognition")] public List<RecognitionObj> RecognitionObjects;
    }
    [Serializable]
    public class RecognitionObj
    {
        public string Name;
        public GameObject[] OnGameObject;
        public GameObject[] OffGameObject;
    }
}