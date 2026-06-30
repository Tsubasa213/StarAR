using System.Collections.Generic;
using UnityEngine;
using System.Globalization;

public class StarGenerator : MonoBehaviour
{
    public GameObject starPrefab;

    [SerializeField]
    private float celestialRadius = 100f;

    void Start()
    {
        GenerateStars();
    }

    void GenerateStars()
    {
        TextAsset csvFile = Resources.Load<TextAsset>("stars_6");
        Debug.Log(csvFile.text.Substring(0, 300));
        if (csvFile == null)
        {
            Debug.LogError("stars_6.csv が見つかりません");
            return;
        }

        string[] lines = csvFile.text.Split('\n');

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            Debug.Log($"===== 行 {i} =====");

            string[] cols = lines[i].Split(',');

            try
            {
                float ra = float.Parse(cols[0], CultureInfo.InvariantCulture);
                float dec = float.Parse(cols[1], CultureInfo.InvariantCulture);
                float mag = float.Parse(cols[4], CultureInfo.InvariantCulture);
                float bv = 0.5f;

                if (!string.IsNullOrWhiteSpace(cols[5]))
                {
                    bv = float.Parse(cols[5], CultureInfo.InvariantCulture);
                }

                CreateStar(ra, dec, mag, bv);
            }
            catch
            {
                Debug.LogError($"エラー行: {i}");
                Debug.LogError(lines[i]);
                return;
            }
        }

        Debug.Log("星生成完了");
    }

    void CreateStar(float raDeg, float decDeg, float mag, float bv)
    {
        float ra = raDeg * Mathf.Deg2Rad;
        float dec = decDeg * Mathf.Deg2Rad;

        float x =
            celestialRadius *
            Mathf.Cos(dec) *
            Mathf.Cos(ra);

        float y =
            celestialRadius *
            Mathf.Sin(dec);

        float z =
            celestialRadius *
            Mathf.Cos(dec) *
            Mathf.Sin(ra);

        GameObject star =
            Instantiate(
                starPrefab,
                new Vector3(x, y, z),
                Quaternion.identity,
                transform

            
            );

        //-------------------------
        // 等級によるサイズ変更
        //-------------------------

        float scale =
            Mathf.Lerp(
                0.1f,      // 5等星
                0.7f,      // 明るい星
                Mathf.Clamp01((3f - mag) / 3f)
            );

        star.transform.localScale =
            Vector3.one * scale;

        //-------------------------
        // B-Vによる色変更
        //-------------------------

        Renderer renderer =
            star.GetComponent<Renderer>();

        Color color = GetStarColor(bv);

        renderer.material.SetColor(
            "_BaseColor",
            color * 2.5f
        );


        }

    Color GetStarColor(float bv)
    {
        if (bv < 0f)
        {
            // 青白い星
            return new Color(
                0.7f,
                0.8f,
                1.0f
            );
        }

        if (bv < 0.5f)
        {
            // 白
            return Color.white;
        }

        if (bv < 1.0f)
        {
            // 黄色
            return new Color(
                1.0f,
                1.0f,
                0.7f
            );
        }

        // 赤っぽい星
        return new Color(
            1.0f,
            0.6f,
            0.5f
        );
    }
}