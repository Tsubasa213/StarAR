using System.Collections.Generic;
using UnityEngine;
using System.Globalization;

public class StarGenerator : MonoBehaviour
{
    public GameObject starPrefab;

    [SerializeField]
    private float celestialRadius = 100f;

    [Tooltip("Resources内の星表名。stars_5は約5等星まで、stars_6は約6等星まで読み込みます。")]
    [SerializeField]
    private string starCatalogResourceName = "stars";

    // 追加: 赤道座標系の基準となる親オブジェクト
    [SerializeField]
    private Transform celestialSphere;

    void Start()
    {
        GenerateStars();
    }

    void GenerateStars()
    {
        string resourceName = string.IsNullOrWhiteSpace(starCatalogResourceName)
            ? "stars_5"
            : starCatalogResourceName.Trim();

        TextAsset csvFile = Resources.Load<TextAsset>(resourceName);
        if (csvFile == null)
        {
            Debug.LogError($"星表 {resourceName}.csv が見つかりません");
            return;
        }

        Debug.Log($"StarGenerator: {resourceName}.csv を読み込みます。");

        string[] lines = csvFile.text.Split('\n');

        int generatedCount = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] cols = line.Split(',');

            try
            {
                if (cols.Length < 6)
                {
                    Debug.LogWarning($"列不足のためスキップ: {i}");
                    continue;
                }

                float ra = float.Parse(cols[0], CultureInfo.InvariantCulture);
                float dec = float.Parse(cols[1], CultureInfo.InvariantCulture);
                float mag = float.Parse(cols[4], CultureInfo.InvariantCulture);
                float bv = 0.5f;

                if (!string.IsNullOrWhiteSpace(cols[5]))
                {
                    bv = float.Parse(cols[5], CultureInfo.InvariantCulture);
                }

                CreateStar(ra, dec, mag, bv);
                generatedCount++;
            }
            catch
            {
                Debug.LogError($"エラー行: {i}");
                Debug.LogError(line);
            }
        }

        Debug.Log(
            $"StarGenerator: 星生成完了。catalog={resourceName}, count={generatedCount}");
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

        // 変更: transform ではなく celestialSphere を親として生成する
        GameObject star =
            Instantiate(
                starPrefab,
                new Vector3(x, y, z),
                Quaternion.identity,
                celestialSphere
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
