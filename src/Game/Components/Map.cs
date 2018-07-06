﻿//Copyright(c) 2018 Daniel Bramblett, Daniel Dupriest, Brandon Goldbeck

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ecs;
using Game.DungeonMaker; 
using Game.DataStructures;
using IO;

namespace Game.Components
{
    class Map : Component
    {

        private int width;
        private int height;
        private List<List<CellState>> cellGrid;
        private List<List<GameObject>> objectGrid;
        public int startingX = 0;
        public int startingY = 0;

        public Map(int width, int height)
        {
            this.width = width;
            this.height = height;

            // Initialize cell grid
            cellGrid = new List<List<CellState>>();
            for (int x = 0; x < width; ++x)
            {
                List<CellState> row = new List<CellState>();
                for (int y = 0; y < height; ++y)
                {
                    row.Add(CellState.Open);
                }
                this.cellGrid.Add(row);
            }

            // Initialize object grid
            objectGrid = new List<List<GameObject>>();
            for (int x = 0; x < width; ++x)
            {
                List<GameObject> row = new List<GameObject>();
                for (int y = 0; y < height; ++y)
                {
                    row.Add(null);
                }
                this.objectGrid.Add(row);
            }
        }

        public override void Start()
        {
            Model mapModel = (Model)this.gameObject.AddComponent(new Model());
            CreateLevel(1);
            return;
        }

        public override void Update()
        {
            Model mapModel = (Model)gameObject.GetComponent<Model>();
            List<List<String>> colorModel = new List<List<String>>();
            List<String> updated = new List<String>();
            
            for (int y = height - 1; y >= 0; --y)
            {
                StringBuilder sb = new StringBuilder();
                List<String> colorBufferRow = new List<String>();

                for (int x = 0; x < width; ++x)
                {
                    if (cellGrid[x][y] == CellState.Blocked)
                        sb.Append("█");
                    else
                        sb.Append(" ");
                    // In the future, we can color walls and doors n stuff differently.
                    colorBufferRow.Add("\u001b[30;1m");
                }
                colorModel.Add(colorBufferRow);
                updated.Add(sb.ToString());
            }
            mapModel.model = updated;
            mapModel.colorModel = colorModel;

            return;
        }

        public override void Render()
        {
            return;
        }

        private void CreateLevel(int level)
        {
            BasicDungeon dm = new BasicDungeon(this.width, this.height, (int)DateTime.Now.Ticks);
            dm.Generate();

            SpawnManager sm = new SpawnManager();

            List<List<String>> blueprint = dm.Package();
            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    switch (blueprint[x][y])
                    {
                        case "d":
                            objectGrid[x][y] = sm.CreateDoor(x, y);
                            break;
                        case "m":
                            objectGrid[x][y] = sm.CreateEnemy(x, y, level);
                            break;
                        case "w":
                            cellGrid[x][y] = CellState.Blocked;
                            break;
                        case "s":
                            this.startingX = x;
                            this.startingY = y;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public CellState GetCellState(int x, int y)
        {
            Debug.Log("GetCellState called with x = " + x + ", y = " + y + ".");
            return cellGrid[x][y];
        }

        public GameObject PeekObject(int x, int y)
        {
            Debug.Log("PeekObject called with x = " + x + ", y = " + y + ".");
            return objectGrid[x][y];
        }

        public GameObject PopObject(int x, int y)
        {
            Debug.Log("PopObject called with x = " + x + ", y = " + y + ".");
            GameObject result = objectGrid[x][y];
            objectGrid[x][y] = null;
            return result;
        }
    }
}