using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TestMeshScript : MonoBehaviour {

    protected const int MAX_PLATE_SIZE_X  = 210 / 5;//210mmがタミヤが販売している最大サイズです
    protected const int MAX_PLATE_SIZE_Y  = 210 / 5;//210mmがタミヤが販売している最大サイズです
    protected const int DIV_DRAW_LIMIT_XY = 210 / 5;

    /// <summary>
    /// 1/4 サイズのパーツデータ
    /// </summary>
    /// <remarks>
    /// この単位を扱うデータの最小単位とします
    /// </remarks>
    protected class QuarterInfo_
    {
        /// <summary>
        /// ブロックの形状
        /// </summary>
        public enum BlockTypes{
            /// <summary>穴用ブロック</summary>
            Hole,
            /// <summary>マージン用ブロック</summary>
            Margin,
        };
        /// <summary>
        /// ブロックの向き
        /// </summary>
        public enum RotateDirs
        {
            DEG_0   = 0,
            DEG_90  = 1,
            DEG_180 = 2,
            DEG_270 = 3,
        };
        public BlockTypes BlockType;
        public RotateDirs RotateDir;
    }
    protected class PlateDrawObject
    {
        PlateDrawObject()
        {
            gameobject = new GameObject();
            gameobject.AddComponent<MeshFilter>();
            gameobject.AddComponent<MeshRenderer>();
        }
        GameObject gameobject;
        Mesh       mesh;
    }

    protected QuarterInfo_[,]   plateBuildMap_;
    protected PlateDrawObject[] plateDrawObject_ = new PlateDrawObject[4];

    #region mesh サポート
    void SetRectangleVtx_(int[] triangles,int startTriIdx, int v0, int v1, int v2, int v3)
    {
        triangles[startTriIdx*3+0] = v0;
        triangles[startTriIdx*3+1] = v1;
        triangles[startTriIdx*3+2] = v2;
        triangles[startTriIdx*3+3] = v2;
        triangles[startTriIdx*3+4] = v3;
        triangles[startTriIdx*3+5] = v0;
    }
    #endregion

    #region CreateQuarterBlock_Hole()
    Mesh CreateQuarterBlock_Hole()
    {
        const float SIZE_XZ = 2.5f;
        const float MARGIN_LEN = 1.0f;
        const float HOLE_R = 1.5f;
        const float SIZE_Y = 3;
        const int DIV_R = 4;

        var mesh = new Mesh();

        var vertices  = new Vector3[6*2 + 4*4 + (DIV_R+1)*4];
        var triangles = new int[(((DIV_R)*4)+8*2) * 3];

        //MEMO: UnityのMESHは頂点法線を別インデックスとしては持てないみたいなので頂点多めに…

        //底面
        vertices[0] = new Vector3(0,0,0);
        vertices[1] = new Vector3(SIZE_XZ, 0, 0);
        vertices[2] = new Vector3(SIZE_XZ, 0, MARGIN_LEN);
        vertices[3] = new Vector3(MARGIN_LEN, 0, MARGIN_LEN);
        vertices[4] = new Vector3(MARGIN_LEN, 0, SIZE_XZ);
        vertices[5] = new Vector3(0, 0, SIZE_XZ);

        //上面
        vertices[6 + 0] = new Vector3(0, SIZE_Y, 0);
        vertices[6 + 1] = new Vector3(SIZE_XZ, SIZE_Y, 0);
        vertices[6 + 2] = new Vector3(SIZE_XZ, SIZE_Y, MARGIN_LEN);
        vertices[6 + 3] = new Vector3(MARGIN_LEN, SIZE_Y, MARGIN_LEN);
        vertices[6 + 4] = new Vector3(MARGIN_LEN, SIZE_Y, SIZE_XZ);
        vertices[6 + 5] = new Vector3(0, SIZE_Y, SIZE_XZ);

        // 側面1
        vertices[12+4*0+0] = vertices[0]; 
        vertices[12+4*0+1] = vertices[6+0]; 
        vertices[12+4*0+2] = vertices[6+1]; 
        vertices[12+4*0+3] = vertices[1];

        // 側面2
        vertices[12+4*1+0] = vertices[1]; 
        vertices[12+4*1+1] = vertices[6+1]; 
        vertices[12+4*1+2] = vertices[6+2]; 
        vertices[12+4*1+3] = vertices[2];

        // 側面3
        vertices[12+4*2+0] = vertices[0]; 
        vertices[12+4*2+1] = vertices[5]; 
        vertices[12+4*2+2] = vertices[6+5]; 
        vertices[12+4*2+3] = vertices[6+0];

        // 側面4
        vertices[12+4*3+0] = vertices[5]; 
        vertices[12+4*3+1] = vertices[4]; 
        vertices[12+4*3+2] = vertices[6+4]; 
        vertices[12+4*3+3] = vertices[6+5];

        // 穴の部分
        for (int ii = 0; ii < DIV_R+1; ii++)
        {
            var rC = Mathf.Cos((Mathf.PI / 2) * ((float)ii / DIV_R)) * HOLE_R;
            var rS = Mathf.Sin((Mathf.PI / 2) * ((float)ii / DIV_R)) * HOLE_R;

            vertices[12+16 + ii            ] = new Vector3(SIZE_XZ-rC, 0,      SIZE_XZ-rS);
            vertices[12+16 + ii + (DIV_R+1)] = new Vector3(SIZE_XZ-rC, SIZE_Y, SIZE_XZ-rS);
            vertices[12+16+(DIV_R+1)*2 + ii            ] = vertices[12+16 + ii            ];
            vertices[12+16+(DIV_R+1)*2 + ii + (DIV_R+1)] = vertices[12+16 + ii + (DIV_R+1)];
        }
        // 面
        SetRectangleVtx_(triangles, 0,  0, 1, 2, 3);
        SetRectangleVtx_(triangles, 2,  3, 4, 5, 0);
        SetRectangleVtx_(triangles, 4,  6 + 0,  6 + 3,  6 + 2,  6 + 1);
        SetRectangleVtx_(triangles, 6,  6 + 3,  6 + 0,  6 + 5,  6 + 4);

        int baseIdx=12;
        SetRectangleVtx_(triangles, 8, baseIdx + 0, baseIdx + 1, baseIdx + 2, baseIdx + 3);
        baseIdx += 4;
        SetRectangleVtx_(triangles,10, baseIdx + 0, baseIdx + 1, baseIdx + 2, baseIdx + 3);
        baseIdx += 4;
        SetRectangleVtx_(triangles,12, baseIdx + 0, baseIdx + 1, baseIdx + 2, baseIdx + 3);
        baseIdx += 4;
        SetRectangleVtx_(triangles,14, baseIdx + 0, baseIdx + 1, baseIdx + 2, baseIdx + 3);
        baseIdx += 4;

        for (int ii = 0; ii < DIV_R; ii++)
        {
            SetRectangleVtx_(triangles, 16+ii*4,
                baseIdx + ii,
                baseIdx + ii + 1,
                baseIdx + ii + (DIV_R + 1) + 1,
                baseIdx + ii + (DIV_R + 1));
            triangles[(16+ii*4+2) * 3 + 0] = 3;
            triangles[(16+ii*4+2) * 3 + 1] = baseIdx+(DIV_R + 1)*2 +ii+1;
            triangles[(16+ii*4+2) * 3 + 2] = baseIdx+(DIV_R + 1)*2 +ii;

            triangles[(16+ii*4+3) * 3 + 0] = 6 + 3;
            triangles[(16+ii*4+3) * 3 + 1] = baseIdx+(DIV_R + 1)*2 + ii + (DIV_R + 1);
            triangles[(16+ii*4+3) * 3 + 2] = baseIdx+(DIV_R + 1)*2 + ii + (DIV_R + 1)+1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
       
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;

    }
    #endregion


    /// <summary>
    /// 基本プレートを構築します
    /// </summary>
    /// <param name="plateSizeX">5mm(穴１個相当)を１単位としたXサイズを指定します</param>
    /// <param name="plateSizeY">5mm(穴１個相当)を１単位としたYサイズを指定します</param>
    /// <remarks>販売されている2.5mmの余白が付いたプレートを生成します</remarks>
    protected void BuildMap_BasicPlate_(int plateSizeX, int plateSizeY)
    {
        int plateQSizeX = plateSizeX * 2;
        int plateQSizeZ = plateSizeY * 2;

        var hole4x4DirMap = new QuarterInfo_.RotateDirs[2, 2]{
            {QuarterInfo_.RotateDirs.DEG_0,  QuarterInfo_.RotateDirs.DEG_270,},
            {QuarterInfo_.RotateDirs.DEG_90, QuarterInfo_.RotateDirs.DEG_180,},
        };

        plateBuildMap_ = new QuarterInfo_[plateQSizeZ, plateQSizeX];
        for (int zz = 0; zz < plateBuildMap_.GetLength(0); zz++)
        {
            for (int xx = 0; xx < plateBuildMap_.GetLength(1); xx++)
            {
                if (xx == 0 || zz == 0 ||
                    (xx == plateBuildMap_.GetLength(1) - 1) ||
                    (zz == plateBuildMap_.GetLength(0) - 1))
                {
                    // 余白
                    plateBuildMap_[zz, xx] = new QuarterInfo_()
                    {
                        BlockType = QuarterInfo_.BlockTypes.Margin,
                    };
                }
                else
                {
                    // 穴
                    plateBuildMap_[zz, xx] = new QuarterInfo_()
                    {
                        BlockType = QuarterInfo_.BlockTypes.Hole,
                        RotateDir = hole4x4DirMap[(zz + 1) % 2, (xx + 1) % 2],
                    };
                    Debug.Log(plateBuildMap_[zz, xx].BlockType.ToString());
                }
            }
        }
        

    }

    // Use this for initialization
    void Start()
    {
        BuildMap_BasicPlate_(12,32);

        {
            plateMesh_ = new Mesh();
            var combineInsLst = new List<CombineInstance>();
            for (int zz = 0; zz < plateBuildMap_.GetLength(0); zz++)
            {
                for (int xx = 0; xx < plateBuildMap_.GetLength(1); xx++)
                {
                    var qinfo = plateBuildMap_[zz,xx];

                    var mtx = Matrix4x4.TRS(new Vector3(-2.5f/2, 0, -2.5f/2), Quaternion.identity, Vector3.one);
                    mtx = Matrix4x4.TRS(new Vector3(xx * 2.5f, 0, zz * 2.5f), Quaternion.Euler(0, (int)qinfo.RotateDir * 90, 0), Vector3.one) * mtx;

                    //Debug.Log(string.Format("{0} {1} {2} {3}", qinfo.RotateDir, qinfo.BlockType, xx * 2.5f, 0, zz * 2.5f));
                    
                    combineInsLst.Add(new CombineInstance()
                    {
                        mesh = CreateQuarterBlock_Hole(),
                        transform = mtx,
                    });
                }
            }

            int vNum = 0;
            foreach (var c in combineInsLst)
            {
                vNum += c.mesh.vertexCount;
            }
            Debug.Log(string.Format("vNum={0} {1}", vNum, plateBuildMap_.GetLength(0) * plateBuildMap_.GetLength(1)));


            plateMesh_.CombineMeshes(combineInsLst.ToArray());
            GetComponent<MeshFilter>().sharedMesh = plateMesh_;
            GetComponent<MeshFilter>().sharedMesh.name = "myMesh";
        }
    }
	
	// Update is called once per frame
	void Update () {
	
	}
}
