using System;
using System.Collections.Generic;
using Godot;

public class Global : Node
{
    [Signal]
    public delegate void NewLightingSetting(LightingSetting lightingSetting);

    public const string LIGHTING_QUALITY_SETTING = "application/game/lighting_quality";
    public static LightingSetting CurrentLightingSetting
    {
        get => (LightingSetting)ProjectSettings.GetSetting(LIGHTING_QUALITY_SETTING); set
        {
            ProjectSettings.SetSetting(LIGHTING_QUALITY_SETTING, (int)value);
            if (singleton != null)
            {
                singleton.EmitSignal(nameof(NewLightingSetting), value);
            }
        }
    }

    public const string DEFAULT_DIFFICULTY_SETTING = "application/game/default_difficulty";
    public static GameDifficulty CurrentDefaultDifficultySetting
    {
        get => (GameDifficulty)ProjectSettings.GetSetting(DEFAULT_DIFFICULTY_SETTING);
        set => ProjectSettings.SetSetting(DEFAULT_DIFFICULTY_SETTING, (int)value);
    }

    private const string SAVE_DIRECTORY = "user://";
    private const string SAVE_FILE_PREFIX = "save-";
    private const string SAVE_FILE_SUFFIX = ".json";

    private const string SETTINGS_SAVE_FILE = "settings";
    private const string DEFAULT_SAVE_FILE = "default";

    private const string BEST_LEVEL_KEY = "bestLevels";
    private const string LEVEL_RECORDS_KEY = "levelRecords";

    private const string MASTER_VOLUME_KEY = "masterVolume";
    private const string SFX_VOLUME_KEY = "sfxVolume";
    private const string MUSIC_VOLUME_KEY = "musicVolume";
    private const string QUALITY_KEY = "quality";

    private const int FIRST_LEVEL = 1;
    private static int currentLevel = FIRST_LEVEL;
    public static int CurrentLevel { get => currentLevel; }

    private static string currentLevelPath = GetLevelScenePath(currentLevel);
    public static string CurrentLevelPath { get => currentLevelPath; }

    public static GameDifficulty Difficulty = GameDifficulty.EASY;

    private static readonly int[] bestLevels = new int[] { FIRST_LEVEL, FIRST_LEVEL, FIRST_LEVEL };
    public static int BestLevel => BestLevelForDifficulty(Difficulty);
    public static int BestLevelForDifficulty(GameDifficulty difficulty) => bestLevels[(int)difficulty];

    private static readonly int[][] bestLevelTimes = MakeBlankBestLevelTimes();
    public static int GetBestLevelTime(int levelIndex, GameDifficulty difficulty) => bestLevelTimes[levelIndex][(int)difficulty];

    private static bool hasBeatenAllLevels = false;
    public static bool HasBeatenAllLevels { get => hasBeatenAllLevels; }

    private static GameSettings settings;
    public static GameSettings Settings { get => settings; }

    public static bool ReturningToMenu = false;

    private static Global singleton;
    public static Global SINGLETON => singleton;

    public override void _Ready()
    {
        singleton = this;
        // if (save_file_exists(SETTINGS_SAVE_FILE))
        // {
        //     settings.initialize_from_dictionary(load_data(SETTINGS_SAVE_FILE));
        // }
    }

    public static void SetLevelToBest()
    {
        currentLevel = BestLevel;
        currentLevelPath = GetLevelScenePath(currentLevel);
    }

    public static void SetLevel(int levelId)
    {
        currentLevel = levelId;
        currentLevelPath = GetLevelScenePath(currentLevel);
    }

    public static void SetLevelToFirst()
    {
        currentLevel = FIRST_LEVEL;
        currentLevelPath = GetLevelScenePath(currentLevel);
    }

    public static bool WinLevel(int level, int time)
    {
        currentLevel = level;
        int levelIndex = level - 1;
        bestLevelTimes[levelIndex][(int)Difficulty] = bestLevelTimes[levelIndex][(int)Difficulty] == -1
            ? time
            : Mathf.Min(bestLevelTimes[levelIndex][(int)Difficulty], time);

        int lastLevel = currentLevel;
        string nextLevelPath = GetLevelScenePath(currentLevel + 1);
        bool hasNextLevel = false;
        if (new File().FileExists(nextLevelPath))
        {
            ++currentLevel;
            currentLevelPath = nextLevelPath;
            bestLevels[(int)Difficulty] = Mathf.Max(bestLevels[(int)Difficulty], currentLevel);
            hasNextLevel = true;
        }
        else
        {
            hasBeatenAllLevels = true;
            SetLevelToFirst();
        }
        SaveRecordData();
        return hasNextLevel;
    }

    private static string GetLevelScenePath(int level)
    {
        return "res://scenes/Level" + level + ".tscn";
    }

    public static void SaveRecordData()
    {
        var data = new Dictionary<string, object>();
        var array = new Godot.Collections.Array();
        foreach (int[] bestTimes in bestLevelTimes)
        {
            array.Add(new Godot.Collections.Array(bestTimes[0], bestTimes[1], bestTimes[2]));
        }
        data.Add(BEST_LEVEL_KEY, array);
        SaveData(BEST_LEVEL_KEY, data);
    }

    public static bool LoadRecordData()
    {
        var data = LoadData(BEST_LEVEL_KEY);
        if (data == null || !data.Contains(BEST_LEVEL_KEY))
        {
            return false;
        }
        var result = data[BEST_LEVEL_KEY] as Godot.Collections.Array;
        for (int levelIndex = 0; levelIndex < bestLevelTimes.Length && levelIndex < result.Count; levelIndex++)
        {
            var resultTimes = result[levelIndex] as Godot.Collections.Array;
            if (resultTimes == null)
            {
                return false;
            }
            for (int difficultyIndex = 0; difficultyIndex < 3 && difficultyIndex < resultTimes.Count; difficultyIndex++)
            {
                bestLevelTimes[levelIndex][difficultyIndex] = Convert.ToInt32(resultTimes[difficultyIndex]);
                if (bestLevelTimes[levelIndex][difficultyIndex] != -1 && levelIndex < bestLevelTimes.Length - 1)
                {
                    bestLevels[difficultyIndex] = Mathf.Max(bestLevels[difficultyIndex], levelIndex + 2);
                }
            }
        }
        return true;
    }

    private static int[][] MakeBlankBestLevelTimes()
    {
        int levelCount = GetLevelCount();
        int[][] result = new int[levelCount][];
        for (int i = 0; i < levelCount; i++)
        {
            result[i] = new int[] { -1, -1, -1 };
        }
        return result;
    }

    private static int[] MakeBlankBestLevels()
    {
        return new int[] { FIRST_LEVEL, FIRST_LEVEL, FIRST_LEVEL };
    }

    public static int GetLevelCount()
    {
        for (int i = FIRST_LEVEL; i <= 100; i++)
        {
            if (!new File().FileExists(GetLevelScenePath(i + 1)))
            {
                return i - FIRST_LEVEL + 1;
            }
        }
        throw new Exception("WTF");
    }

    public static void SaveSettings()
    {
        var data = new Dictionary<string, object>();
        data.Add(MASTER_VOLUME_KEY, settings.MasterVolume);
        data.Add(SFX_VOLUME_KEY, settings.SfxVolume);
        data.Add(MUSIC_VOLUME_KEY, settings.MusicVolume);
        data.Add(QUALITY_KEY, (int)settings.Quality);
        SaveData(SETTINGS_SAVE_FILE, data);
    }

    public static void LoadSettings()
    {
        if (settings != null)
        {
            return;
        }
        settings = new GameSettings();
        var data = LoadData(SETTINGS_SAVE_FILE);
        if (data == null)
        {
            GD.Print("No data for settings could be loaded");
            return;
        }

        // Generally ignoring exceptions like this is a bad idea, but keys not being present is to be expected;

        try { settings.MasterVolume = Convert.ToInt32(data[MASTER_VOLUME_KEY]); }
        catch (KeyNotFoundException) { }

        try { settings.SfxVolume = Convert.ToInt32(data[SFX_VOLUME_KEY]); }
        catch (KeyNotFoundException) { }

        try { settings.MusicVolume = Convert.ToInt32(data[MUSIC_VOLUME_KEY]); }
        catch (KeyNotFoundException) { }

        try { settings.Quality = (GameQuality)(Convert.ToInt32(data[QUALITY_KEY])); }
        catch (InvalidCastException)
        {
            GD.PushError(String.Format("Failed to cast the raw quality value '{0}' to a GameQuality enum", data["quality"]));
        }
        catch (KeyNotFoundException) { }
    }

    private static void SaveData(string save_name, Dictionary<string, object> data)
    {
        var path = GetUserJsonFilePath(save_name);
        // LOG.info("Saving data to: %s" % path);
        var file = new File();
        var openError = file.Open(path, File.ModeFlags.Write);
        if (openError != 0)
        {
            GD.Print("Open of ", path, " error: ", openError);
            return;
        }
        // LOG.check_error_code(file.open(path, File.WRITE), "Opening '%s'" % path);
        // LOG.info("Saving to: " + file.get_path_absolute());
        file.StoreString(JSON.Print(data));
        file.Close();
    }

    private string[] GetSaveFiles()
    {
        var dir = OpenSaveDirectory();
        dir.ListDirBegin(false, false);
        // LOG.check_error_code(dir.list_dir_begin(false, false), "Listing the files of " + SAVE_DIRECTORY);
        var file_name = dir.GetNext();

        List<string> result = new List<string>();
        while (file_name != "")
        {
            if (!dir.CurrentIsDir() && file_name.StartsWith(SAVE_FILE_PREFIX)
                    && file_name.EndsWith(SAVE_FILE_SUFFIX))
            {
                result.Add(file_name.Substr(SAVE_FILE_PREFIX.Length,
                    file_name.Length - SAVE_FILE_PREFIX.Length - SAVE_FILE_SUFFIX.Length));
            }
            file_name = dir.GetNext();
        }
        dir.ListDirEnd();

        result.Sort();
        return result.ToArray();
    }

    Node GetSingleNodeInGroup(string group)
    {
        Godot.Collections.Array nodes = GetTree().GetNodesInGroup(group);
        return nodes.Count > 0 ? (Node)nodes[0] : null;
    }

    private Directory OpenSaveDirectory()
    {
        var dir = new Directory();
        // LOG.check_error_code(dir.open(SAVE_DIRECTORY), "Opening " + SAVE_DIRECTORY);
        return dir;
    }

    private bool LoadGame(string save_name = DEFAULT_SAVE_FILE)
    {
        if (!SaveFileExists(save_name))
        {
            return false;
        }
        var loaded_data = LoadData(save_name);
        if (loaded_data.Count == 0)
        {
            return false;
        }
        var game_data = loaded_data;
        currentLevel = (int)(game_data["level"] ?? FIRST_LEVEL);
        return true;
    }

    private static bool SaveFileExists(string save_name)
    {
        var path = GetUserJsonFilePath(save_name);
        return new File().FileExists(path);
    }

    private static Godot.Collections.Dictionary LoadData(string fileName)
    {
        var path = GetUserJsonFilePath(fileName);
        var file = new File();
        if (!file.FileExists(path))
        {
            return null;
        }
        Error openStatus = file.Open(path, File.ModeFlags.Read);
        if (openStatus != Error.Ok)
        {
            GD.Print("Failed to open file ", fileName, "; error code: ", openStatus);
            return null;
        }
        // LOG.check_error_code(file.open(path, File.READ), "Opening file " + path);
        var raw_data = file.GetAsText();
        file.Close();
        var loaded_data = raw_data != null ? JSON.Parse(raw_data) : null;
        if (loaded_data.Result != null && loaded_data.Result is Godot.Collections.Dictionary)
        {
            return (Godot.Collections.Dictionary)loaded_data.Result;
        }
        else
        {
            GD.PushWarning(String.Format("Corrupted data in file '{0}'!", path));
            return null;
        }
    }

    private static string GetUserJsonFilePath(string save_name)
    {
        return SAVE_DIRECTORY + save_name + ".json";
    }
}

public class GameSettings
{
    public int MasterVolume = 20;
    public float NormalizedMasterVolume { get => MasterVolume * .01f; }

    public int SfxVolume = 80;
    public float NormalizedSfxVolume { get => SfxVolume * .01f; }

    public int MusicVolume = 80;
    public float NormalizedMusicVolume { get => MusicVolume * .01f; }

    public GameQuality Quality = GameQuality.MEDIUM;
}

public enum GameQuality
{
    LOW, MEDIUM, HIGH
}

public enum GameDifficulty
{
    EASY, MEDIUM, HARD
}
