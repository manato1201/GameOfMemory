
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


namespace DB
{

    [CreateAssetMenu(fileName = "DB_Explanation", menuName = "Scriptable Objects/DB_Explanation")]
    public class DB_Explanation : ScriptableObject
    {

        #region QOL向上処理
        // CakeParamsSOが保存してある場所のパス
        public const string PATH = "DB_Explanation";

        // CakeParamsDBの実体
        private static DB_Explanation _entity = null;
        public static DB_Explanation Entity
        {
            get
            {
                // 初アクセス時にロードする
                if (_entity == null)
                {
                    _entity = Resources.Load<DB_Explanation>(PATH);

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

        [Header("DB_Explanation")] public List<ExplanationObj> ExplanationObjects;
    }
    [Serializable]
    public class ExplanationObj
    {
        public string Name;
        public string AreaName;   // 説明エリアの名前
        public string UIName;     // 表示するUIオブジェクトの名前
    }
}