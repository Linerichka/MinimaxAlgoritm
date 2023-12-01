using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AI 
{
    public class AIController : MonoBehaviour
    {
        private sbyte _aiLevel = 8;

        private sbyte _mapWidth = 8;
        private sbyte _mapHeight = 8;
        private sbyte _wolfCount = 4;
        private bool _debugging = true;
        #region ScriptLogic
        long _countGetMoveWeight = 0;

        private enum _moveWeight : sbyte
        {
            VerySmall = 1,
            Small = 2,
            Medium = 3,
            Normal = 4,
            Big = 6,
            VeryBig = 8,
            ExstraBig = 12,
            Max = 127
        }

        private SbyteVector2[] _possibleSheepMoves;
        private SbyteVector2[] _possibleWolfMoves;
        private SbyteVector2[] _startPositionsOnMap; //all eneme pos on turn event
        #endregion


        #region set values and add listining
        private void Awake()
        {
            SetValue();         
        }

        private void OnEnable()
        {
            EventBus.AITurn += AITurnHandler;
        }

        private void OnDisable()
        {
            EventBus.AITurn -= AITurnHandler;
        }       

        private void Start()
        {
            EventBus.AIEnabled?.Invoke();
        }

        private void SetValue()
        {          
            // x y 
            //�� �� ������������ ����� ���� ��������� ������ ���� ��� ���� ������ ���� ������� �� ���� �� ���  y - 1
            _possibleSheepMoves = new SbyteVector2[]
            {
            new SbyteVector2(-1, -1),
            new SbyteVector2(+1, -1),
            new SbyteVector2(-1, +1),
            new SbyteVector2(+1, +1)
            };
            
            _possibleWolfMoves = new SbyteVector2[]
            {
            new SbyteVector2(-1, +1),
            new SbyteVector2(+1, +1)
            };
        }
        #endregion


        private void AITurnHandler(List<Vector2> positions)
        {
            _startPositionsOnMap = SbyteVector2.ConvertVectorListToSbyteVectorArray(positions);
            float time = Time.realtimeSinceStartup; // debugging       
            sbyte indexBestMove = GetIndexBestMove();

            if (_debugging)
            {
                Debug.LogWarning(_countGetMoveWeight + " time: " + (Time.realtimeSinceStartup - time) + " operation/time: " + (_countGetMoveWeight / (Time.realtimeSinceStartup - time)));
            }

            _countGetMoveWeight = 0;
            MoveEntityInGamePositionByIndexBestMove(indexBestMove, _startPositionsOnMap);
        }

        private sbyte GetIndexBestMove()
        {
            sbyte bestMoveIndex = -1;
            short bestMoveWeight = -1500;

            
            ///����������� ����������
            ///���������� ����������� ������������ ������� �� � �������� ������ ���
            for (sbyte i = 1; i <= _aiLevel; i++)
            {
                short[] result = RunMinMax(true, 0, _startPositionsOnMap, aiLevel: i);

                if (_debugging)
                {
                    Debug.Log($"Ai level: {i}  move index: {result[0]}  weight: {result[1]}");
                }

                ///� ������ ���� ��� ����� ������� ��������� ����, ��� ������ ��� ���� ��������� ������ ��� ���
                ///������� ���, ����������� ������������ ��� ����������
                if (result[1] == 1500 || result[1] == -1500) break;

                //���� ��� ���� ������ �������, �� ������� ���.
                if (result[1] > bestMoveWeight)
                {
                    bestMoveIndex = (sbyte)result[0];
                    bestMoveWeight = result[1];
                }
            }

            return bestMoveIndex;
        }

        private short[] RunMinMax(bool AI, sbyte recursiveLevel, in SbyteVector2[] startPositions, short bestMoveWeightAlpha = -1500, short badMoveWeightBeta = 1500, in sbyte aiLevel = 3)
        {
            ///���� ������� ������� ������������� �������, �� ������� ���
            ///����� ���������� ��� ���� ����� ���� ��� ���� ������� ���, �� ���� ����� ����� AI == true, �� ������� ���� ����
            ///��� ��� ���� ���� � �������: ���� - ���� -���� - ���� - �������, ����� ������� ������� ���������� �������� ���� ����
            if ((recursiveLevel >= aiLevel * 2) && AI)
            {
                return new short[2] { -1, GetMoveWeight(startPositions) };
            }

            //��������� ���������� � ��� ��� ������� ���� �� ����� ����� ����������������
            short indexBestMove = -1;
            short bestMoveWeight = -1500;
            short badMoveWeight = 1500;

            //������� ��� ��������� �� ��� ��������� ���� � ������� �� ���� �������� RunMinMax()
            //positions: 0-3 wolf,   4 sheep
            for (int moveIndex = 0; moveIndex < (AI ? _possibleSheepMoves.Length : _possibleWolfMoves.Length * _wolfCount); moveIndex++)
            {
                SbyteVector2[] positions; //��������� ������� ������, ������������� ������ ���� �������� �������� �� CanMove()

                if (AI)
                {
                    //������ ��� �� �������� � ������ ��� ����� �������
                    SbyteVector2 newSheepPosition = startPositions[4] + _possibleSheepMoves[moveIndex];

                    //��������� ����� �� ������� ��� �� ����� �������
                    if (!CanMove(newSheepPosition, startPositions)) continue;

                    ///������ "����" ������� �������.
                    ///������������ ��������� ����� ��� ��� Array.Clone() �������� ������� ������ ������ �� �� ��������� ���������� �����,
                    ///������������ ����� � ���� ������� ������ ����� ������ ������� ��������� ����������� � ������� ��� � �� ������� �������.
                    positions = SbyteVector2.CloneSbyteVectorArray(startPositions);
                    //������������� ����� ������� ��� ����
                    positions[4] = newSheepPosition;
                }
                else
                {
                    //�������� ������ ����� � ������� �������
                    int wolfIndex = moveIndex / _possibleWolfMoves.Length;
                    //������ ��� �� �������� � ������ ��� ����� �������
                    SbyteVector2 newWolfPosition = startPositions[wolfIndex] + _possibleWolfMoves[moveIndex % _possibleWolfMoves.Length];

                    if (!CanMove(newWolfPosition, startPositions)) continue;

                    positions = SbyteVector2.CloneSbyteVectorArray(startPositions);
                    //������������� ����� ������� ��� �����
                    positions[wolfIndex] = newWolfPosition;
                }

                //������� �����, �������� � ���� ����� ������� � ��������� ������� ��������
                short moveWeight = RunMinMax(!AI, (sbyte)(recursiveLevel + 1),
                    positions, bestMoveWeightAlpha, badMoveWeightBeta, aiLevel)[1];

                //�������� ������ ������ ��� ��� ����� ���� ���������
                if (AI)
                {
                    //���� ������ �� ��� ������� ������ ��� ��� ����
                    if (moveWeight > bestMoveWeightAlpha) bestMoveWeightAlpha = moveWeight;
                }
                else
                {
                    //���� ���� �� �� ������� ������ ��� ��� ����
                    if (moveWeight < badMoveWeightBeta) badMoveWeightBeta = moveWeight;
                }

                //�� ������� �������� ���� ��� ������ ������� �� ���������� ���� ��� ��� ������ ��� ����
                if (recursiveLevel <= 0 && moveWeight >= bestMoveWeight)
                {
                    //������� ����������� ���� ���� ����������
                    if (moveWeight == bestMoveWeight)
                    {
                        if (GetRandomBool())
                        {
                            indexBestMove = (short)moveIndex;
                        }
                    }
                    else
                    {
                        indexBestMove = (short)moveIndex;
                    }
                }               

                //���������� ������ � ������ ��� ����
                if (moveWeight > bestMoveWeight) bestMoveWeight = moveWeight;         
                if (moveWeight < badMoveWeight) badMoveWeight = moveWeight;
                
                //�����-���� ���������          
                if (bestMoveWeightAlpha >= badMoveWeightBeta)
                {
                    break;
                }
            }

            //���� ���� �� ��� ������� ������ ��� ��� ����, ���� ���� �� �� ������� ������ ��� ��� ����
            return AI ? new short[2] { indexBestMove, bestMoveWeight } : new short[2] { -1, badMoveWeight };
        }

        private short GetMoveWeight(in SbyteVector2[] positions)
        {
            _countGetMoveWeight++;
            short moveWeight = 0;
            //��������� ��� ��� ������, ��� ����� ���� � ���� �����
            moveWeight += (short)((_mapHeight - 1 - positions[4].y) * (sbyte)_moveWeight.Normal);

            bool sheepLover = true;

            for (sbyte i = 0; i < _wolfCount; i++)
            {
                //���� ���� ���� ����
                if (positions[i].y < positions[4].y)
                {
                    sheepLover = false;
                    break;
                }
            }

            ///���� ������ �� ���� ����� ��� ���� �� ����� ����� � ����� ����� ������,
            ///�� ���� ������������ ��� ��� ����� ����, ��� ��� ��� ������� ������
            if (positions[4].y == 0 || sheepLover)
            {
                moveWeight += (sbyte)_moveWeight.Max;
            }

            //��������� ������ ��� ��������� �����
            sbyte moveCell = 0;

            for (sbyte sheepMoveIndex = 0; sheepMoveIndex < _possibleSheepMoves.Length; sheepMoveIndex++)
            {
                if (!CanMove(positions[4] + _possibleSheepMoves[sheepMoveIndex], positions)) continue;

                moveCell++;
            }

            //���� ����� ��� �� ������ ������� ��� ����, ��� ��� ��� �������� ��������� ��� ����
            if (moveCell == 0)
            {
                moveWeight -= (sbyte)_moveWeight.Max;
            }

            return moveWeight;
        }

        private bool CanMove(in SbyteVector2 newPosition, in SbyteVector2[] position)
        {
            //������� false ���� x ��� y ��������� �� �������� �����
            if (!(newPosition.x < _mapWidth && newPosition.y < _mapHeight && newPosition.x >= 0 && newPosition.y >= 0)) return false;

            //��������� ���� �� ����� �������� �� ����� �������
            for (sbyte i = 0; i < position.Length; i++)
            {
                if (position[i] == newPosition) return false;
            }

            return true;
        }

        #region Move Entity In Game Position
        private void MoveEntityInGamePositionByMapKey(sbyte x, sbyte y)
        {
            Vector3[] pathArray = { new Vector3(-(x * 2), 0, -(y * 2)) };
            EventBus.CharacterMoveStarted?.Invoke(pathArray);
        }

        private void MoveEntityInGamePositionByIndexBestMove(sbyte indexBestMove, SbyteVector2[] positions)
        {
            SbyteVector2 newSheepBestPositionOnMap = positions[4] + _possibleSheepMoves[indexBestMove];
            MoveEntityInGamePositionByMapKey(newSheepBestPositionOnMap.x, newSheepBestPositionOnMap.y);
        }
        #endregion

        private bool GetRandomBool()
        {
            return (_countGetMoveWeight & 1) == 0;
        }
    }

    //�� ���� ������� Vector2, �� ������������ �� float � sbyte ��������
    internal struct SbyteVector2
    {
        public sbyte x;
        public sbyte y;

        #region create sbyte vector
        public SbyteVector2(in sbyte x, in sbyte y)
        {
            this.x = x;
            this.y = y;
        }
        public SbyteVector2(in Vector2 vector2)
        {
            this.x = (sbyte)vector2.x;
            this.y = (sbyte)vector2.y;
        }
        public SbyteVector2(in SbyteVector2 sbyteVector2)
        {
            this.x = sbyteVector2.x;
            this.y = sbyteVector2.y;
        }
        #endregion

        #region Static metods
        public static List<SbyteVector2> ConvertVectorListToSbyteVectorList(in List<Vector2> vector2List)
        {
            List<SbyteVector2> result = new List<SbyteVector2>();

            foreach (Vector2 vector2 in vector2List)
            {
                result.Add(new SbyteVector2(vector2));
            }

            return result;
        }

        public static SbyteVector2[] ConvertVectorListToSbyteVectorArray(in List<Vector2> vector2List)
        {
            SbyteVector2[] result = new SbyteVector2[vector2List.Count];

            for (int i = 0; i < vector2List.Count; i++)
            {
                result[i] = new SbyteVector2(vector2List[i]);
            }

            return result;
        }

        public static SbyteVector2[] ConvertVectorArrayToSbyteVectorArray(in Vector2[] vector2Array)
        {
            SbyteVector2[] result = new SbyteVector2[vector2Array.Count()];

            for (int i = 0; i < vector2Array.Count(); i++)
            {
                result[i] = new SbyteVector2(vector2Array[i]);
            }

            return result;
        }

        public static SbyteVector2[] CloneSbyteVectorArray(in SbyteVector2[] sbyteVector2)
        {
            SbyteVector2[] result = new SbyteVector2[sbyteVector2.Length];

            for (int i = 0; i < sbyteVector2.Length; i++)
            {
                result[i] = sbyteVector2[i];
            }

            return result;
        }
        #endregion

        #region operators
        public static SbyteVector2 operator +(in SbyteVector2 a, in SbyteVector2 b)
        {
            return new SbyteVector2((sbyte)(a.x + b.x), (sbyte)(a.y + b.y));
        }

        public static SbyteVector2 operator -(in SbyteVector2 a, in SbyteVector2 b)
        {
            return new SbyteVector2((sbyte)(a.x - b.x), (sbyte)(a.y - b.y));
        }

        public static SbyteVector2 operator -(in SbyteVector2 a)
        {
            return new SbyteVector2((sbyte)(a.x * -1), (sbyte)(a.y * -1));
        }

        public static bool operator ==(in SbyteVector2 a, in SbyteVector2 b)
        {
            return (a.x == b.x && a.y == b.y);
        }

        public static bool operator !=(in SbyteVector2 a, in SbyteVector2 b)
        {
            return !(a == b);
        }
        #endregion

        #region override metods
        public override string ToString() => $"x: {x}, y: {y}";
        
        public override bool Equals(object obj) => ((SbyteVector2)obj) == this;
        
        public override int GetHashCode() => base.GetHashCode();
        #endregion
    }
}
