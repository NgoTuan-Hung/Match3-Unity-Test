using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public class Board
{
    public enum eMatchDirection
    {
        NONE,
        HORIZONTAL,
        VERTICAL,
        ALL,
    }

    private int boardSizeX;

    private int boardSizeY;
    public int BottomCellCount { get; private set; }

    private Cell[,] m_cells;
    private Cell[] m_bottomCells;
    public int TotalBottomItem { get; private set; }
    public int RemainedBoardItem { get; private set; }
    private Transform m_root;
    private int m_matchMin;
    Dictionary<NormalItem.eNormalType, int> m_bottomTypesCount =
        new Dictionary<NormalItem.eNormalType, int>();

    public Board(Transform transform, GameSettings gameSettings)
    {
        m_root = transform;

        m_matchMin = gameSettings.MatchesMin;

        this.boardSizeX = gameSettings.BoardSizeX;
        this.boardSizeY = gameSettings.BoardSizeY;
        this.BottomCellCount = gameSettings.BottomCellCount;
        this.RemainedBoardItem = boardSizeX * boardSizeY;

        m_cells = new Cell[boardSizeX, boardSizeY];

        CreateBoard();
    }

    private void CreateBoard()
    {
        Vector3 origin = new Vector3(-boardSizeX * 0.5f + 0.5f, -boardSizeY * 0.5f + 0.5f, 0f);
        GameObject prefabBG = Resources.Load<GameObject>(Constants.PREFAB_CELL_BACKGROUND);
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                GameObject go = GameObject.Instantiate(prefabBG);
                go.transform.position = origin + new Vector3(x, y, 0f);
                go.transform.SetParent(m_root);

                Cell cell = go.GetComponent<Cell>();
                cell.Setup(x, y);

                m_cells[x, y] = cell;
            }
        }

        //set neighbours
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                if (y + 1 < boardSizeY)
                    m_cells[x, y].NeighbourUp = m_cells[x, y + 1];
                if (x + 1 < boardSizeX)
                    m_cells[x, y].NeighbourRight = m_cells[x + 1, y];
                if (y > 0)
                    m_cells[x, y].NeighbourBottom = m_cells[x, y - 1];
                if (x > 0)
                    m_cells[x, y].NeighbourLeft = m_cells[x - 1, y];
            }
        }

        CreateBottomCells(prefabBG);
    }

    private void CreateBottomCells(GameObject cellPF)
    {
        var origin = new Vector3(-BottomCellCount * 0.5f + 0.5f, -boardSizeY * 0.5f - 1.5f, 0f);
        m_bottomCells = new Cell[BottomCellCount];
        for (int i = 0; i < BottomCellCount; i++)
        {
            GameObject go = GameObject.Instantiate(cellPF);
            go.transform.position = new Vector3(origin.x + i, origin.y, 0f);
            go.transform.SetParent(m_root);

            m_bottomCells[i] = go.GetComponent<Cell>();
            m_bottomCells[i].IsBottomCell = true;
        }
    }

    internal void Fill()
    {
        GenerateTypesPool();

        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                NormalItem item = new NormalItem();

                List<NormalItem.eNormalType> types = new List<NormalItem.eNormalType>();
                if (cell.NeighbourBottom != null)
                {
                    NormalItem nitem = cell.NeighbourBottom.Item as NormalItem;
                    if (nitem != null)
                    {
                        types.Add(nitem.ItemType);
                    }
                }

                if (cell.NeighbourLeft != null)
                {
                    NormalItem nitem = cell.NeighbourLeft.Item as NormalItem;
                    if (nitem != null)
                    {
                        types.Add(nitem.ItemType);
                    }
                }

                // item.SetType(Utils.GetRandomNormalTypeExcept(types.ToArray()));
                // item.SetView();
                // item.SetViewRoot(m_root);

                cell.Assign(item);
                // cell.ApplyItemPosition(false);
            }
        }

        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                NormalItem item = cell.Item as NormalItem;

                item.SetType(GetRandomTypeFromPool());
                item.SetView();
                item.SetViewRoot(m_root);

                cell.ApplyItemPosition(false);
            }
        }
    }

    Dictionary<NormalItem.eNormalType, int> typesPool =
        new Dictionary<NormalItem.eNormalType, int>();

    void GenerateTypesPool()
    {
        typesPool.Clear();
        var count = boardSizeX * boardSizeY / 3;

        var allNormalTypes = Utils.GetAllNormalTypes();
        for (int i = 0; i < allNormalTypes.Length; i++)
        {
            typesPool.Add(allNormalTypes[i], 3);
        }

        for (int i = allNormalTypes.Length; i < count; i++)
        {
            var type = Utils.GetRandomNormalType();
            typesPool[type] += 3;
        }

        // for (int i = 0; i < count; i++)
        // {
        //     var type = Utils.GetRandomNormalType();
        //     if (!typesPool.ContainsKey(type))
        //         typesPool.Add(type, 3);
        //     else
        //         typesPool[type] += 3;
        // }
    }

    NormalItem.eNormalType GetRandomTypeFromPool()
    {
        if (typesPool.Count == 0)
            throw new System.Exception("Types pool is empty!");

        int total = 0;

        // Sum all remaining counts
        foreach (var pair in typesPool)
            total += pair.Value;

        int random = UnityEngine.Random.Range(0, total);
        int current = 0;

        foreach (var pair in typesPool)
        {
            current += pair.Value;

            if (random < current)
            {
                typesPool[pair.Key]--;

                if (typesPool[pair.Key] <= 0)
                    typesPool.Remove(pair.Key);

                return pair.Key;
            }
        }

        throw new System.Exception("Random selection failed.");
    }

    internal void Shuffle()
    {
        List<Item> list = new List<Item>();
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                list.Add(m_cells[x, y].Item);
                m_cells[x, y].Free();
            }
        }

        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                int rnd = UnityEngine.Random.Range(0, list.Count);
                m_cells[x, y].Assign(list[rnd]);
                m_cells[x, y].ApplyItemMoveToPosition();

                list.RemoveAt(rnd);
            }
        }
    }

    internal void FillGapsWithNewItems()
    {
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                if (!cell.IsEmpty)
                    continue;

                NormalItem item = new NormalItem();

                item.SetType(Utils.GetRandomNormalType());
                item.SetView();
                item.SetViewRoot(m_root);

                cell.Assign(item);
                cell.ApplyItemPosition(true);
            }
        }
    }

    internal void ExplodeAllItems()
    {
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                cell.ExplodeItem();
            }
        }
    }

    public void Swap(Cell cell1, Cell cell2, Action callback)
    {
        Item item = cell1.Item;
        cell1.Free();
        Item item2 = cell2.Item;
        cell1.Assign(item2);
        cell2.Free();
        cell2.Assign(item);

        item.View.DOMove(cell2.transform.position, 0.3f);
        item2
            .View.DOMove(cell1.transform.position, 0.3f)
            .OnComplete(() =>
            {
                if (callback != null)
                    callback();
            });
    }

    public void PutItemToBottom(Cell cell, Action gameOverCallback, Action gameWinCallback)
    {
        if (TotalBottomItem >= BottomCellCount || cell.IsEmpty)
        {
            return;
        }

        RemainedBoardItem--;

        var item = cell.Item as NormalItem;
        cell.Free();

        if (!m_bottomTypesCount.ContainsKey(item.ItemType))
        {
            m_bottomTypesCount.Add(item.ItemType, 0);
        }

        int putIndex = TotalBottomItem++;
        if (m_bottomTypesCount[item.ItemType] >= 1)
        {
            for (int i = TotalBottomItem - 2; i >= 0; i--)
            {
                if (m_bottomCells[i].Item.IsSameType(item))
                {
                    putIndex = i + 1;
                    if (!m_bottomCells[putIndex].IsEmpty)
                    {
                        ShiftBottomItemsToRight(putIndex);
                    }
                    break;
                }
            }
        }

        m_bottomCells[putIndex].Assign(item);
        item.View.DOMove(m_bottomCells[putIndex].transform.position, 0.3f)
            .OnComplete(() =>
                HandlePlayBottomCell(item, putIndex, gameOverCallback, gameWinCallback)
            );
    }

    void ShiftBottomItemsToRight(int index)
    {
        for (int i = TotalBottomItem - 2; i >= index; i--)
        {
            TransferItem(m_bottomCells[i], m_bottomCells[i + 1]);
        }
    }

    void ShiftBottomItemsToLeft(int startIndex, int shiftValue)
    {
        for (int i = startIndex; i < TotalBottomItem; i++)
        {
            TransferItem(m_bottomCells[i], m_bottomCells[i - shiftValue]);
        }
    }

    void TransferItem(Cell from, Cell to)
    {
        Item item = from.Item;
        from.Free();
        to.Assign(item);
        item.View.transform.position = to.transform.position;
    }

    void HandlePlayBottomCell(
        NormalItem item,
        int putIndex,
        Action gameOverCallback,
        Action gameWinCallback
    )
    {
        if (m_bottomTypesCount[item.ItemType] == 2)
        {
            for (int i = 0; i < 3; i++)
                m_bottomCells[putIndex - i].ExplodeItem();

            ShiftBottomItemsToLeft(putIndex + 1, 3);

            m_bottomTypesCount[item.ItemType] = 0;
            TotalBottomItem -= 3;

            if (RemainedBoardItem == 0)
                gameWinCallback?.Invoke();
        }
        else
            m_bottomTypesCount[item.ItemType]++;

        if (TotalBottomItem == BottomCellCount)
            gameOverCallback?.Invoke();
    }

    public List<Cell> GetHorizontalMatches(Cell cell)
    {
        List<Cell> list = new List<Cell>();
        list.Add(cell);

        //check horizontal match
        Cell newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourRight;
            if (neib == null)
                break;

            if (neib.IsSameType(cell))
            {
                list.Add(neib);
                newcell = neib;
            }
            else
                break;
        }

        newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourLeft;
            if (neib == null)
                break;

            if (neib.IsSameType(cell))
            {
                list.Add(neib);
                newcell = neib;
            }
            else
                break;
        }

        return list;
    }

    public List<Cell> GetVerticalMatches(Cell cell)
    {
        List<Cell> list = new List<Cell>();
        list.Add(cell);

        Cell newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourUp;
            if (neib == null)
                break;

            if (neib.IsSameType(cell))
            {
                list.Add(neib);
                newcell = neib;
            }
            else
                break;
        }

        newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourBottom;
            if (neib == null)
                break;

            if (neib.IsSameType(cell))
            {
                list.Add(neib);
                newcell = neib;
            }
            else
                break;
        }

        return list;
    }

    internal void ConvertNormalToBonus(List<Cell> matches, Cell cellToConvert)
    {
        eMatchDirection dir = GetMatchDirection(matches);

        BonusItem item = new BonusItem();
        switch (dir)
        {
            case eMatchDirection.ALL:
                item.SetType(BonusItem.eBonusType.ALL);
                break;
            case eMatchDirection.HORIZONTAL:
                item.SetType(BonusItem.eBonusType.HORIZONTAL);
                break;
            case eMatchDirection.VERTICAL:
                item.SetType(BonusItem.eBonusType.VERTICAL);
                break;
        }

        if (item != null)
        {
            if (cellToConvert == null)
            {
                int rnd = UnityEngine.Random.Range(0, matches.Count);
                cellToConvert = matches[rnd];
            }

            item.SetView();
            item.SetViewRoot(m_root);

            cellToConvert.Free();
            cellToConvert.Assign(item);
            cellToConvert.ApplyItemPosition(true);
        }
    }

    internal eMatchDirection GetMatchDirection(List<Cell> matches)
    {
        if (matches == null || matches.Count < m_matchMin)
            return eMatchDirection.NONE;

        var listH = matches.Where(x => x.BoardX == matches[0].BoardX).ToList();
        if (listH.Count == matches.Count)
        {
            return eMatchDirection.VERTICAL;
        }

        var listV = matches.Where(x => x.BoardY == matches[0].BoardY).ToList();
        if (listV.Count == matches.Count)
        {
            return eMatchDirection.HORIZONTAL;
        }

        if (matches.Count > 5)
        {
            return eMatchDirection.ALL;
        }

        return eMatchDirection.NONE;
    }

    internal List<Cell> FindFirstMatch()
    {
        List<Cell> list = new List<Cell>();

        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];

                var listhor = GetHorizontalMatches(cell);
                if (listhor.Count >= m_matchMin)
                {
                    list = listhor;
                    break;
                }

                var listvert = GetVerticalMatches(cell);
                if (listvert.Count >= m_matchMin)
                {
                    list = listvert;
                    break;
                }
            }
        }

        return list;
    }

    public List<Cell> CheckBonusIfCompatible(List<Cell> matches)
    {
        var dir = GetMatchDirection(matches);

        var bonus = matches.Where(x => x.Item is BonusItem).FirstOrDefault();
        if (bonus == null)
        {
            return matches;
        }

        List<Cell> result = new List<Cell>();
        switch (dir)
        {
            case eMatchDirection.HORIZONTAL:
                foreach (var cell in matches)
                {
                    BonusItem item = cell.Item as BonusItem;
                    if (item == null || item.ItemType == BonusItem.eBonusType.HORIZONTAL)
                    {
                        result.Add(cell);
                    }
                }
                break;
            case eMatchDirection.VERTICAL:
                foreach (var cell in matches)
                {
                    BonusItem item = cell.Item as BonusItem;
                    if (item == null || item.ItemType == BonusItem.eBonusType.VERTICAL)
                    {
                        result.Add(cell);
                    }
                }
                break;
            case eMatchDirection.ALL:
                foreach (var cell in matches)
                {
                    BonusItem item = cell.Item as BonusItem;
                    if (item == null || item.ItemType == BonusItem.eBonusType.ALL)
                    {
                        result.Add(cell);
                    }
                }
                break;
        }

        return result;
    }

    internal List<Cell> GetPotentialMatches()
    {
        List<Cell> result = new List<Cell>();
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];

                //check right
                /* example *\
                  * * * * *
                  * * * * *
                  * * * ? *
                  * & & * ?
                  * * * ? *
                \* example  */

                if (cell.NeighbourRight != null)
                {
                    result = GetPotentialMatch(
                        cell,
                        cell.NeighbourRight,
                        cell.NeighbourRight.NeighbourRight
                    );
                    if (result.Count > 0)
                    {
                        break;
                    }
                }

                //check up
                /* example *\
                  * ? * * *
                  ? * ? * *
                  * & * * *
                  * & * * *
                  * * * * *
                \* example  */
                if (cell.NeighbourUp != null)
                {
                    result = GetPotentialMatch(
                        cell,
                        cell.NeighbourUp,
                        cell.NeighbourUp.NeighbourUp
                    );
                    if (result.Count > 0)
                    {
                        break;
                    }
                }

                //check bottom
                /* example *\
                  * * * * *
                  * & * * *
                  * & * * *
                  ? * ? * *
                  * ? * * *
                \* example  */
                if (cell.NeighbourBottom != null)
                {
                    result = GetPotentialMatch(
                        cell,
                        cell.NeighbourBottom,
                        cell.NeighbourBottom.NeighbourBottom
                    );
                    if (result.Count > 0)
                    {
                        break;
                    }
                }

                //check left
                /* example *\
                  * * * * *
                  * * * * *
                  * ? * * *
                  ? * & & *
                  * ? * * *
                \* example  */
                if (cell.NeighbourLeft != null)
                {
                    result = GetPotentialMatch(
                        cell,
                        cell.NeighbourLeft,
                        cell.NeighbourLeft.NeighbourLeft
                    );
                    if (result.Count > 0)
                    {
                        break;
                    }
                }

                /* example *\
                  * * * * *
                  * * * * *
                  * * ? * *
                  * & * & *
                  * * ? * *
                \* example  */
                Cell neib = cell.NeighbourRight;
                if (
                    neib != null
                    && neib.NeighbourRight != null
                    && neib.NeighbourRight.IsSameType(cell)
                )
                {
                    Cell second = LookForTheSecondCellVertical(neib, cell);
                    if (second != null)
                    {
                        result.Add(cell);
                        result.Add(neib.NeighbourRight);
                        result.Add(second);
                        break;
                    }
                }

                /* example *\
                  * * * * *
                  * & * * *
                  ? * ? * *
                  * & * * *
                  * * * * *
                \* example  */
                neib = null;
                neib = cell.NeighbourUp;
                if (neib != null && neib.NeighbourUp != null && neib.NeighbourUp.IsSameType(cell))
                {
                    Cell second = LookForTheSecondCellHorizontal(neib, cell);
                    if (second != null)
                    {
                        result.Add(cell);
                        result.Add(neib.NeighbourUp);
                        result.Add(second);
                        break;
                    }
                }
            }

            if (result.Count > 0)
                break;
        }

        return result;
    }

    private List<Cell> GetPotentialMatch(Cell cell, Cell neighbour, Cell target)
    {
        List<Cell> result = new List<Cell>();

        if (neighbour != null && neighbour.IsSameType(cell))
        {
            Cell third = LookForTheThirdCell(target, neighbour);
            if (third != null)
            {
                result.Add(cell);
                result.Add(neighbour);
                result.Add(third);
            }
        }

        return result;
    }

    private Cell LookForTheSecondCellHorizontal(Cell target, Cell main)
    {
        if (target == null)
            return null;
        if (target.IsSameType(main))
            return null;

        //look right
        Cell second = null;
        second = target.NeighbourRight;
        if (second != null && second.IsSameType(main))
        {
            return second;
        }

        //look left
        second = null;
        second = target.NeighbourLeft;
        if (second != null && second.IsSameType(main))
        {
            return second;
        }

        return null;
    }

    private Cell LookForTheSecondCellVertical(Cell target, Cell main)
    {
        if (target == null)
            return null;
        if (target.IsSameType(main))
            return null;

        //look up
        Cell second = target.NeighbourUp;
        if (second != null && second.IsSameType(main))
        {
            return second;
        }

        //look bottom
        second = null;
        second = target.NeighbourBottom;
        if (second != null && second.IsSameType(main))
        {
            return second;
        }

        return null;
    }

    private Cell LookForTheThirdCell(Cell target, Cell main)
    {
        if (target == null)
            return null;
        if (target.IsSameType(main))
            return null;

        //look up
        Cell third = CheckThirdCell(target.NeighbourUp, main);
        if (third != null)
        {
            return third;
        }

        //look right
        third = null;
        third = CheckThirdCell(target.NeighbourRight, main);
        if (third != null)
        {
            return third;
        }

        //look bottom
        third = null;
        third = CheckThirdCell(target.NeighbourBottom, main);
        if (third != null)
        {
            return third;
        }

        //look left
        third = null;
        third = CheckThirdCell(target.NeighbourLeft, main);
        ;
        if (third != null)
        {
            return third;
        }

        return null;
    }

    private Cell CheckThirdCell(Cell target, Cell main)
    {
        if (target != null && target != main && target.IsSameType(main))
        {
            return target;
        }

        return null;
    }

    internal void ShiftDownItems()
    {
        for (int x = 0; x < boardSizeX; x++)
        {
            int shifts = 0;
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                if (cell.IsEmpty)
                {
                    shifts++;
                    continue;
                }

                if (shifts == 0)
                    continue;

                Cell holder = m_cells[x, y - shifts];

                Item item = cell.Item;
                cell.Free();

                holder.Assign(item);
                item.View.DOMove(holder.transform.position, 0.3f);
            }
        }
    }

    public void Clear()
    {
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                cell.Clear();

                GameObject.Destroy(cell.gameObject);
                m_cells[x, y] = null;
            }
        }
    }

    public void PutRandomItemToBottom(Action gameOverCallback, Action gameWinCallback)
    {
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                if (!cell.IsEmpty)
                {
                    PutItemToBottom(cell, gameOverCallback, gameWinCallback);
                    return;
                }
            }
        }
    }

    public NormalItem.eNormalType GetBottomItemType(int index) =>
        (m_bottomCells[index].Item as NormalItem).ItemType;

    public void PutIdenticalItemToBottom(
        NormalItem.eNormalType eNormalType,
        Action gameOverCallback,
        Action gameWinCallback
    )
    {
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                if (!cell.IsEmpty && (cell.Item as NormalItem).ItemType == eNormalType)
                {
                    PutItemToBottom(cell, gameOverCallback, gameWinCallback);
                    return;
                }
            }
        }
    }

    public void PutUniqueItemToBottom(Action gameOverCallback, Action gameWinCallback)
    {
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];
                if (!cell.IsEmpty && IsItemUniqueInBottom((cell.Item as NormalItem).ItemType))
                {
                    PutItemToBottom(cell, gameOverCallback, gameWinCallback);
                    return;
                }
            }
        }
    }

    bool IsItemUniqueInBottom(NormalItem.eNormalType eNormalType)
    {
        for (int i = 0; i < TotalBottomItem; i++)
            if ((m_bottomCells[i].Item as NormalItem).ItemType == eNormalType)
                return false;
        return true;
    }
}
