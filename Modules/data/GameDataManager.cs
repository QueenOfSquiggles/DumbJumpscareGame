
using System.Text.Json;

namespace data
{
    public partial class GameDataManager : Node
    {
        [Signal] public delegate void OnPhaseChangedEventHandler(int nPhase);
        [Signal] public delegate void OnActiveGeneratorsChangedEventHandler(int nGenerators);
        [Signal] public delegate void OnFoundKeysChangedEventHandler(int nKeys);
        [Signal] public delegate void OnPuzzleSolvedEventHandler();


        public static GameDataManager instance { get; set; }
        private GameData game_data = new();


        [Export]
        public int GamePhase
        {
            // C# properties are mega based
            get
            // C# properties are mega based
            => game_data.phase;
            set
            {
                _ = EmitSignal(nameof(OnPhaseChanged), value);
                game_data.phase = value;
            }
        }

        [Export]
        public int ActiveGenerators
        {
            get => game_data.active_generators;
            set
            {
                _ = EmitSignal(nameof(OnActiveGeneratorsChanged), value);
                game_data.active_generators = value;
                CheckGamePhase();
            }
        }

        [Export]
        public bool PuzzleSolved
        {
            get => game_data.puzzle_solved;
            set
            {
                if (value)
                {
                    _ = EmitSignal(nameof(OnPuzzleSolved), value);
                }

                game_data.puzzle_solved = value;
                CheckGamePhase();
            }
        }

        [Export]
        public int FoundKeys
        {
            get => game_data.found_keys;
            set
            {
                _ = EmitSignal(nameof(OnFoundKeysChanged), value);
                game_data.found_keys = value;
                CheckGamePhase();
            }
        }
        public int GameAggression
        {
            get => game_data.aggression_level;
            set => game_data.aggression_level = value;
        }
        public int RequiredGenerators
        {
            get => game_data.required_generators;
            set => game_data.required_generators = value;
        }
        public int RequiredKeys
        {
            get => game_data.required_keys;
            set => game_data.required_keys = value;
        }


        public int LevelGenerators
        {
            get => game_data.level_generators;
            set
            {
                game_data.level_generators = value;
                if (game_data.required_generators > value)
                {
                    game_data.required_generators = value;
                }
            }
        }
        public int LevelChests
        {
            get => game_data.level_chests;
            set
            {
                game_data.level_chests = value;
                if (game_data.required_keys > value)
                {
                    game_data.required_keys = value;
                }
            }
        }

        private const string FILE_PATH = "user://settings.json";

        public override void _Ready()
        {
            if (instance != null)
            {
                QueueFree();
            }
            else
            {
                instance = this;
            }

            GD.Print("Loading -> GameDataManager:");
            Deserialize();
            GD.Print("Loading -> GameDataManager: Deserialized");
            Reset();
            GD.Print("Loading -> GameDataManager: Complete");
        }

        private void CheckGamePhase()
        {
            bool flag = false;
            switch (game_data.phase)
            {
                case GameData.PHASE_GENERATORS:
                    flag = CheckPhaseGenerators();
                    break;
                case GameData.PHASE_PUZZLE:
                    flag = CheckPhasePuzzle();
                    break;
                case GameData.PHASE_KEYS:
                    flag = CheckPhaseKeys();
                    break;
                default:
                    break;
            }

            if (flag)
            {
                GamePhase += 1; // will trigger signal
                GD.Print($"GameDataManager has progressed to phase #{GamePhase}");
            }
        }

        private bool CheckPhaseGenerators()
        {
            return game_data.active_generators >= game_data.required_generators;
        }

        private bool CheckPhasePuzzle()
        {
            return game_data.puzzle_solved;
        }

        private bool CheckPhaseKeys()
        {
            return game_data.found_keys >= game_data.required_keys;
        }

        public void Reset()
        {
            game_data.active_generators = 0;
            game_data.puzzle_solved = false;
            game_data.found_keys = 0;
            game_data.phase = GameData.PHASE_GENERATORS;
        }


        // IO

        public void Serialize()
        {
            // pull data from AudioServer
            game_data.VolumeMain = AudioServer.GetBusVolumeDb(0);
            game_data.VolumeVO = AudioServer.GetBusVolumeDb(1);
            game_data.VolumeSFX = AudioServer.GetBusVolumeDb(2);
            game_data.VolumeCreature = AudioServer.GetBusVolumeDb(3);
            game_data.VolumeDrones = AudioServer.GetBusVolumeDb(4);

            // convert to JSON
            JsonSerializerOptions ops = new() { WriteIndented = true };
            string content = JsonSerializer.Serialize(game_data, ops);
            FileAccess file = FileAccess.Open(FILE_PATH, FileAccess.ModeFlags.Write);
            file.StoreString(content);
            file.Flush(); // maybe forcing a flush will ensure we don't have degenerate data loaded from disk??
        }

        public void Deserialize()
        {
            FileAccess file = FileAccess.Open(FILE_PATH, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                return;
            }

            try
            {
                GameData loaded_data = JsonSerializer.Deserialize<GameData>(file.GetAsText());
                game_data = loaded_data;
            }
            catch (Exception)
            {
                // I actually don't care? 99% of the time this is going to error from the file not existing, or the file being messed with. The next serialization will correct the issue.
                //GD.PushError($"Error loading GameData from JSON : {e.Message}");

                // if anything goes wrong, assign default values
                game_data = new GameData();
                GamePreset preset = GamePreset.DEFAULT;
                RequiredGenerators = preset.required_generators;
                RequiredKeys = preset.required_keys;
                GameAggression = preset.aggression_level;
                // Audio volumes already have default values
            }

            // Push loaded data to AudioServer
            AudioServer.SetBusVolumeDb(0, game_data.VolumeMain);
            AudioServer.SetBusVolumeDb(1, game_data.VolumeVO);
            AudioServer.SetBusVolumeDb(2, game_data.VolumeSFX);
            AudioServer.SetBusVolumeDb(3, game_data.VolumeCreature);
            AudioServer.SetBusVolumeDb(4, game_data.VolumeDrones);
        }

        // Should only be available in Debug builds (in editor and exports with debug enabled). Not accessible in release
        public override void _Input(InputEvent @event)
        {
            if (Engine.IsEditorHint())
            {
                return;
            }

            if (@event is InputEventKey key_event && @event.IsPressed())
            {
                string text = key_event.AsText();
                if (text.StartsWith("Kp"))
                {
                    string numpad = text.Substr(3, 3);
                    switch (numpad)
                    {
                        case "0":
                            GamePhase = 0;
                            break;
                        case "1":
                            GamePhase = 1;
                            break;
                        case "2":
                            GamePhase = 2;
                            break;
                        case "3":
                            GamePhase = 3;
                            break;
                        default:
                            break;
                    }
                }
            }

        }
    }
}
