//                                                                                              SETTINGS
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

//You can change below settings and recompile the script at any time without losing your data!

//### The minimum ammount of turns required to save the game (useful for servers) ###
    int minTurnToSave = 5;

//### The speed of animations when playing or loading the game ###
//Advised numbers: 0 to 20
//0 turns the animations off, after that the larger the number the slower the animation will be
    int animationSpeed = 6;
    int gameLoadSpeed = 6;

//### Some animation settings ###
//true - knights will move in L shape, false - knights will cut the turn
    bool knightsFullMove = true;
    bool knightsJump = true;

//### Sound settings ###
//In the "SoundSettings" section below only change the "SoundName = " or the "Volume = " part, or change any true to false and vice versa
//The list of available SoundName's can be found in the programmable block "Custom Data" section
//The Volume number should range from 0 to 1, if you use a number between 0 and 1 you have to add an F, for example: 0.45F
    bool isSound = true;
    SoundSettings moveSound = new SoundSettings { SoundName = "MusComp_07", Volume = 1 };
    SoundSettings arrivalSound = new SoundSettings { SoundName = "MusComp_02", Volume = 1 };
    SoundSettings checkSound = new SoundSettings { SoundName = "SoundBlockAlert2", Volume = 1 };
    SoundSettings promotionSound = new SoundSettings { SoundName = "MusAlien_02", Volume = 1 };
    SoundSettings captureSound = new SoundSettings { SoundName = "MusBuild_04", Volume = 0.4F };
    SoundSettings moveNotAllowedSound = new SoundSettings { SoundName = "SoundBlockAlert1", Volume = 1 };

    bool isNotificationSound = true;
    SoundSettings notificationSound = new SoundSettings { SoundName = "MusComp_04", Volume = 1 };

    bool isMusic = true;
    SoundSettings gameEndMusic = new SoundSettings { SoundName = "MusFun", Volume = 0.05F };

//### Message display time settings ###
//Advised numbers: 1 to 10
    int errorMessageTime = 2;
    int endOfGameMessageTime = 4;
    int requestMessageTime = 5;



//                                                                                              PROGRAM
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
//|#| in game blocks and group placeholders |#|
IMyProjector[] projectors = new IMyProjector[32];
IMyTextPanel[] wPanels = new IMyTextPanel[5];
IMyTextPanel[] bPanels = new IMyTextPanel[5];
IMyTimerBlock TB_White, TB_Black;
List<IMySoundBlock> soundBlocks = new List<IMySoundBlock>();

//|#| struct used for setting different event sounds and their loudness |#|
struct SoundSettings
{
    public string SoundName;
    public float Volume;
}

//|#| enum used to determine the ChessFigure type |#|
public enum ChessPiece
{
    Pawn,
    Rook,
    Knight,
    Bishop,
    Queen,
    King
}

//|#| class from which an 8x8 chessBoard array is created |#|
public class ChessFigure
{
    public IMyProjector projector { get; }
    public int startX { get; }
    public int startY { get; }
    public ChessPiece type { get; set; }
    public bool isOriginallyPawn { get; set; }
    public bool color { get; }
    public int hasMoved { get; set; }

    public ChessFigure(IMyProjector projector, int startX, int startY, ChessPiece type, bool color)
    {
        this.projector = projector;
        this.startX = startX;
        this.startY = startY;
        this.type = type;
        isOriginallyPawn = (type == ChessPiece.Pawn);
        this.color = color;
        hasMoved = -1;
    }
    public ChessFigure(ChessFigure copy)
    {
        projector = copy.projector;
        startX = copy.startX;
        startY = copy.startY;
        type = copy.type;
        isOriginallyPawn = copy.isOriginallyPawn;
        color = copy.color;
        hasMoved = copy.hasMoved;
    }
}
ChessFigure[,] chessBoard = new ChessFigure[8, 8];

//|#| structure for holding information about a chess move |#|
public struct MoveInfo
{
    public bool isPromotion { get; set; }
    public bool isLCastling { get; set; }
    public bool isSCastling { get; set; }
    public bool isCheckmate { get; set; }
    public bool isCheck { get; set; }
    public bool isCapture { get; set; }
    public bool isDraw { get; set; }
    public bool isEnPassant { get; set; }
    public ChessPiece pieceType { get; set; }
    public ChessPiece promotedTo { get; set; }
    public ChessFigure capturedPiece { get; set; }
    public int sourceX { get; set; }
    public int sourceY { get; set; }
    public int targetX { get; set; }
    public int targetY { get; set; }
}
MoveInfo moveInfo = new MoveInfo();

//|#| variables and real time use structs |#|
int turnNumber = -1;

int gameState = 0;

int menuChoice = 0;

bool whiteTurn = true;

bool promotionFlag = false, animationFlag = false, enterNameFlag = false, loadGameFlag = false, additionalChoiceFlag = false;

//var used to count the ammount of times the program has entered DrawAnimation()
int animationCounter = 0;

//draws: 0 - none, 1 - stalemate, 2 - fiftyMoves, 3 - threefold, 4 - insufficient material, 5 - agreement
//loses: 0 - none, 1 - checkmate, 2 - surrender, 3 - flag
int drawType = 0, wLossType = 0, bLossType = 0;

ChessPiece promotionPiece = ChessPiece.Queen;

//decision vars, used to determine the players move they want to make (first and second position)
int fPosX = -1, fPosY = -1, sPosX = -1, sPosY = -1;

int[] wKingPos = { 3, 0 }, bKingPos = { 3, 7 }, tempChessPiecePos = { -1, -1 };

int[] wRotationPiece = { 0, 0 }, bRotationPiece = { 0, 7 };

//used to check for threefold repetition
List<string> positionKeys = new List<string>();

//used for checking the fiftyMove rule
int fiftyMoveRuleCounter = 0;

//used to determine the turn in which the analysis got modified
int divergenceTurn = -1;

bool[] isClock = new bool[2] { true, true };
bool[] isIncrement = new bool[2] { true, true };
int[] time = new int[2] { 300, 300 };
int[] increment = new int[2] { 3, 3 };
int[] savedTime = new int[2] { 300, 300 };
int[] savedIncrement = new int[2] { 3, 3 };
int[] timeIncrease = new int[2];
int[] incrementIncrease = new int[2];

TimeSpan ElapsedTime = TimeSpan.Zero;

//Array used for auto restoring panels to default states, the values decide how long before the restoration occurs
//0,1 - w,b rotation panels, 2 - w,b increment/time panels, 3 - w,b info panels 
int[] LCDPanelsTimes = new int[4];

//Structures for player requests - number value usually states the turn the request took place
//0,1 - w,b Chess_TakeBack ; 2,3 - w,b drawoffer ; 4,5 - w,b - timeOnOff ; 6,7 - w,b - incrementOnOff ; 8,9 - w,b timeIncrease ; 10,11 - incrementIncrease ; 12 - game too short to save ; 13 - game already saved ; 14 - this name is already used ; 15 - game lost/drawn
int[] messageFlags = new int[16];

//|#| structures for saving and loading chess games |#|
List<string> chessNotation = new List<string>();
List<MoveInfo> chessMoves = new List<MoveInfo>();
List<List<string>> chessNotationsList = new List<List<string>>();
List<List<MoveInfo>> chessMovesList = new List<List<MoveInfo>>();
List<string> chessGamesNameList = new List<string>();

public Program()
{
    wPanels[0] = GridTerminalSystem.GetBlockWithName("LCD_InfoWhite") as IMyTextPanel;
    wPanels[1] = GridTerminalSystem.GetBlockWithName("LCD_IncrementWhite") as IMyTextPanel;
    wPanels[2] = GridTerminalSystem.GetBlockWithName("LCD_TimeWhite") as IMyTextPanel;
    wPanels[3] = GridTerminalSystem.GetBlockWithName("LCD_RotationWhite") as IMyTextPanel;
    wPanels[4] = GridTerminalSystem.GetBlockWithName("LCD_PromotionWhite") as IMyTextPanel;
    bPanels[0] = GridTerminalSystem.GetBlockWithName("LCD_InfoBlack") as IMyTextPanel;
    bPanels[1] = GridTerminalSystem.GetBlockWithName("LCD_IncrementBlack") as IMyTextPanel;
    bPanels[2] = GridTerminalSystem.GetBlockWithName("LCD_TimeBlack") as IMyTextPanel;
    bPanels[3] = GridTerminalSystem.GetBlockWithName("LCD_RotationBlack") as IMyTextPanel;
    bPanels[4] = GridTerminalSystem.GetBlockWithName("LCD_PromotionBlack") as IMyTextPanel;

    foreach (var panel in wPanels)
        panel.ContentType = ContentType.TEXT_AND_IMAGE;
    foreach (var panel in bPanels)
        panel.ContentType = ContentType.TEXT_AND_IMAGE;
    for(int i = 0; i < 5; i++)
        for(int j = 0; j < 2; j++)
            LCD_SetPanelToDefault(j == 0 ? true : false, i);

    TB_White = GridTerminalSystem.GetBlockWithName("TB_White") as IMyTimerBlock;
    TB_Black = GridTerminalSystem.GetBlockWithName("TB_Black") as IMyTimerBlock;

    IMyBlockGroup soundGroup = GridTerminalSystem.GetBlockGroupWithName("GameSounds");
    soundGroup.GetBlocksOfType(soundBlocks);
    
    string projectorName = "";
    for (int i = 0; i < 4; i++)
    {
        for (int j = 0; j < 8; j++)
        {
            projectorName = "P" + (i < 2 ? "W" : "B") + "_" + (char)(65 + j) + (i < 2 ? (char)(49 + i) : (char)(53 + i));
            projectors[8 * i + j] = GridTerminalSystem.GetBlockWithName(projectorName) as IMyProjector;
        }
    }
    Chess_InitializeGame();
    
    string[] storedData = Storage.Split(';');

    wPanels[0].WriteText(Storage);
    if (storedData.Length > 0)
    {
        if (storedData.Length > 0 && storedData[0].Length > 0)
        {
            string[] storedVars = storedData[0].Split(',');
            gameState = int.Parse(storedVars[0]);
            menuChoice = int.Parse(storedVars[1]);
            divergenceTurn = int.Parse(storedVars[2]);
            isClock[0] = bool.Parse(storedVars[3]);
            isClock[1] = bool.Parse(storedVars[4]);
            isIncrement[0] = bool.Parse(storedVars[5]);
            isIncrement[1] = bool.Parse(storedVars[6]);
            time[0] = int.Parse(storedVars[7]);
            time[1] = int.Parse(storedVars[8]);
            increment[0] = int.Parse(storedVars[9]);
            increment[1] = int.Parse(storedVars[10]);
        }
        if (storedData.Length > 3 && storedData[3].Length > 0)
        {
            string[] storedChessNotationsList = storedData[3].Split('/');
            foreach (string notationList in storedChessNotationsList)
            {
                string[] storedInnerChessNotation = notationList.Split(',');
                foreach (string notation in storedInnerChessNotation)
                    chessNotation.Add(notation);
                chessNotationsList.Add(chessNotation.ToList());
                chessNotation.Clear();
            }
        }
        if (storedData.Length > 4 && storedData[4].Length > 0)
        {
            string[] storedChessMovesList = storedData[4].Split('/');
            foreach (string movesList in storedChessMovesList)
            {
                string[] storedInnerChessMoves = movesList.Split('|');
                Program_DeserializeMoveList(storedInnerChessMoves);
                chessMovesList.Add(chessMoves.ToList());
                chessMoves.Clear();
            }
        }
        if (storedData.Length > 1 && storedData[1].Length > 0)
        {
            string[] storedChessNotation = storedData[1].Split(',');
            foreach (string notation in storedChessNotation)
                chessNotation.Add(notation);
        }
        if (storedData.Length > 2 && storedData[2].Length > 0)
        {
            string[] storedChessMoves = storedData[2].Split('|');
            Program_DeserializeMoveList(storedChessMoves);
        }
        if (storedData.Length > 5 && storedData[5].Length > 0)
        {
            string[] storedchessGamesNameList = storedData[5].Split(',');
            foreach (string name in storedchessGamesNameList)
                chessGamesNameList.Add(name);
        }
    }
    
    loadGameFlag = true;
    Runtime.UpdateFrequency |= UpdateFrequency.Update1;

    if (gameState == 1 && (isClock[0] || isClock[1]))
        Runtime.UpdateFrequency |= UpdateFrequency.Update10;

    LCD_UpdateInfoPanels();
}

public ChessFigure Program_DeserializeChessFigure(string storedChessFigure)
{
    string[] properties = storedChessFigure.Split('.');

    int projectorIndex = (int)properties[0][3] - 65 + ((int)properties[0][4] - 49) % 4 * 8;
    IMyProjector projector = projectors[projectorIndex];

    int startX = int.Parse(properties[1]);
    int startY = int.Parse(properties[2]);
    ChessPiece type = (ChessPiece)int.Parse(properties[3]);
    bool isOriginallyPawn = bool.Parse(properties[4]);
    bool color = bool.Parse(properties[5]);
    int hasMoved = int.Parse(properties[6]);

    return new ChessFigure(projector, startX, startY, type, color)
    {
        isOriginallyPawn = isOriginallyPawn,
        hasMoved = hasMoved
    };
}

public void Program_DeserializeMoveList(string[] storedChessMoves)
{
    foreach (string move in storedChessMoves)
    {
        string[] innerList = move.Split(',');
        moveInfo.isPromotion = bool.Parse(innerList[0]);
        moveInfo.isLCastling = bool.Parse(innerList[1]);
        moveInfo.isSCastling = bool.Parse(innerList[2]);
        moveInfo.isCheckmate = bool.Parse(innerList[3]);
        moveInfo.isCheck = bool.Parse(innerList[4]);
        moveInfo.isCapture = bool.Parse(innerList[5]);
        moveInfo.isDraw = bool.Parse(innerList[6]);
        moveInfo.isEnPassant = bool.Parse(innerList[7]);
        moveInfo.pieceType = (ChessPiece)int.Parse(innerList[8]);
        moveInfo.promotedTo = (ChessPiece)int.Parse(innerList[9]);
        moveInfo.capturedPiece = innerList[10] == "null" ? null : Program_DeserializeChessFigure(innerList[10]);
        moveInfo.sourceX = int.Parse(innerList[11]);
        moveInfo.sourceY = int.Parse(innerList[12]);
        moveInfo.targetX = int.Parse(innerList[13]);
        moveInfo.targetY = int.Parse(innerList[14]);
        chessMoves.Add(moveInfo);
    }
}

public void Save()
{
    string storedVars = $"{(messageFlags[15] >= 0 ? 0 : gameState)},{menuChoice},{divergenceTurn},{isClock[0]},{isClock[1]},{isIncrement[0]},{isIncrement[1]},{time[0]},{time[1]},{increment[0]},{increment[1]}";
    
    string storedChessNotation = string.Join(",", chessNotation);
    
    string storedChessMoves;
    List<string> storedInnerList = new List<string>();
    foreach (MoveInfo info in chessMoves)
    {
        string innerList = $"{info.isPromotion},{info.isLCastling},{info.isSCastling},{info.isCheckmate},{info.isCheck},{info.isCapture},{info.isDraw},{info.isEnPassant},{(int)info.pieceType},{(int)info.promotedTo},{(info.capturedPiece != null ? Save_SerializeChessFigure(info.capturedPiece) : "null")},{info.sourceX},{info.sourceY},{info.targetX},{info.targetY}";
        storedInnerList.Add(innerList);
    }
    storedChessMoves = string.Join("|", storedInnerList);
    
    string storedChessNotationsList;
    storedInnerList.Clear();
    foreach (List<string> notation in chessNotationsList)
    {
        string innerList = string.Join(",", notation);
        storedInnerList.Add(innerList);
    }
    storedChessNotationsList = string.Join("/", storedInnerList);

    string storedChessMovesList;
    storedInnerList.Clear();
    List<string> storedOuterList = new List<string>();
    foreach (List<MoveInfo> infoList in chessMovesList)
    {
        foreach (MoveInfo info in infoList)
        {
            string innerList = $"{info.isPromotion},{info.isLCastling},{info.isSCastling},{info.isCheckmate},{info.isCheck},{info.isCapture},{info.isDraw},{info.isEnPassant},{(int)info.pieceType},{(int)info.promotedTo},{(info.capturedPiece != null ? Save_SerializeChessFigure(info.capturedPiece) : "null")},{info.sourceX},{info.sourceY},{info.targetX},{info.targetY}";
            storedInnerList.Add(innerList);
        }
        string outerList = string.Join("|", storedInnerList);
        storedOuterList.Add(outerList);
        storedInnerList.Clear();
    }
    storedChessMovesList = string.Join("/", storedOuterList);

    string storedchessGamesNameList = string.Join(",", chessGamesNameList);
    
    Storage = string.Join(";", storedVars, storedChessNotation, storedChessMoves, storedChessNotationsList, storedChessMovesList, storedchessGamesNameList);
}
 
public string Save_SerializeChessFigure(ChessFigure chessPiece)
{
    return $"{chessPiece.projector.ToString().Substring(chessPiece.projector.ToString().Length - 5)}.{chessPiece.startX}.{chessPiece.startY}.{(int)chessPiece.type}.{chessPiece.isOriginallyPawn}.{chessPiece.color}.{chessPiece.hasMoved}";
}

public void Main(string arg, UpdateType updateSource)
{
    if ((updateSource & UpdateType.Update100) != 0)
        LCD_PanelTimesHandler();
    if (animationFlag && (((updateSource & UpdateType.Update1) != 0)))
        Chess_DrawAnimation(chessBoard[sPosX, sPosY]);
    else if ((updateSource & UpdateType.Update10) != 0)
        Clock_ChessClocksHandler();
    else if (!animationFlag && loadGameFlag && (updateSource & UpdateType.Update1) != 0)
    {
        if (turnNumber != chessMoves.Count - 1)
            Chess_LoadMove();  
        else
        {
            Runtime.UpdateFrequency &= ~UpdateFrequency.Update1;
            loadGameFlag = false;
        }
        LCD_UpdateInfoPanels();
    }
    if (enterNameFlag && messageFlags[14] == -1)
    {
        if (updateSource == UpdateType.Terminal && arg.Length > 0)
        {
            if (!chessGamesNameList.Contains(arg))
            {
                chessGamesNameList.Add(arg);
                enterNameFlag = false;
            }
            else
            {
                Runtime.UpdateFrequency |= UpdateFrequency.Update100;
                messageFlags[14] = 1;
                LCDPanelsTimes[3] = errorMessageTime;
            }
        }
        LCD_UpdateInfoPanels();
    }
    else if (messageFlags[12] == -1 && messageFlags[13] == -1 && messageFlags[14] == -1 && messageFlags[15] == -1 && !animationFlag)
    {
        if (arg.Length == 2 && updateSource == UpdateType.Trigger && !promotionFlag && drawType == 0 && wLossType == 0 && bLossType == 0)
        {
            sPosX = (int)arg[0] - 65;
            sPosY = (int)arg[1] - 49;
            if (fPosX == -1 || chessBoard[fPosX, fPosY] == null || chessBoard[fPosX, fPosY].color != whiteTurn || (chessBoard[sPosX, sPosY] != null && chessBoard[sPosX, sPosY].color == whiteTurn))
            {
                if (chessBoard[sPosX, sPosY] != null && chessBoard[sPosX, sPosY].color == whiteTurn)
                    Chess_UpdateRotationSelection(whiteTurn, sPosX, sPosY);
                fPosX = sPosX;
                fPosY = sPosY;
                sPosX = sPosY = -1;
            }
            else
            {
                if (Chess_IsValidMove(fPosX, fPosY, sPosX, sPosY))
                {
                    moveInfo = default(MoveInfo);
                    ChessFigure chessPiece = chessBoard[fPosX, fPosY];
                    turnNumber++;
                    moveInfo.pieceType = chessPiece.type;
                    moveInfo.sourceX = fPosX;
                    moveInfo.sourceY = fPosY;
                    moveInfo.targetX = sPosX;
                    moveInfo.targetY = sPosY;
                    if (chessPiece.hasMoved == -1)
                        chessPiece.hasMoved = turnNumber;
                    Chess_MoveChessPiece(chessPiece);
                    Chess_UpdateRotationSelection(whiteTurn, sPosX, sPosY);
                    if (animationSpeed > 0 && Math.Max(Math.Abs(moveInfo.targetX - moveInfo.sourceX), Math.Abs(moveInfo.targetY - moveInfo.sourceY)) > 1)
                    {
                        animationFlag = true;
                        Runtime.UpdateFrequency |= UpdateFrequency.Update1;
                    }
                    if (chessPiece.type == ChessPiece.Pawn && (moveInfo.targetY == 0 || moveInfo.targetY == 7))
                    {
                        moveInfo.isPromotion = true;
                        promotionFlag = true;
                        if (whiteTurn)
                        {
                            wPanels[4].FontSize = 10;
                            wPanels[4].TextPadding = 10;
                            wPanels[4].WriteText(promotionPiece.ToString());
                        }
                        else
                        {
                            bPanels[4].FontSize = 10;
                            bPanels[4].TextPadding = 10;
                            bPanels[4].WriteText(promotionPiece.ToString());
                        }
                    }
                    else
                        Chess_PerformMoveAfterMath();
                }
                else if (isSound)
                    Sound_PlayGameSounds(false);
            }
            LCD_UpdateInfoPanels();
        }
        else if (arg.Length > 2)
        {
            switch (arg)
            {
                case "wPromotion_enter":
                case "wPromotion_next":
                case "wPromotion_prev":
                case "bPromotion_enter":
                case "bPromotion_next":
                case "bPromotion_prev":
                    if (moveInfo.isPromotion)
                        Chess_PromotionHandler(arg);
                    break;
                case "wRotation_scroll":
                case "wRotation_next":
                case "wRotation_prev":
                case "bRotation_scroll":
                case "bRotation_next":
                case "bRotation_prev":
                    Chess_RotationHandler(arg);
                    break;
                case "clocksOn":
                case "clocksOff":
                case "wTime_onOff":
                case "bTime_onOff":
                case "wTime_increase":
                case "bTime_increase":
                case "wTime_decrease":
                case "bTime_decrease":
                case "wIncrement_onOff":
                case "bIncrement_onOff":
                case "wIncrement_increase":
                case "bIncrement_increase":
                case "wIncrement_decrease":
                case "bIncrement_decrease":
                    Input_ClockSettingsHandler(arg);
                    break;
                case "wGreenButton":
                case "bGreenButton":
                case "wOrangeButton":
                case "bOrangeButton":
                case "wRedButton":
                case "bRedButton":
                    Input_SpecialButtonsHandler(arg);
                    break;
            }
        }
    }
}

public void Chess_PerformMoveAfterMath()
{
    whiteTurn = !whiteTurn;
    moveInfo.isCheckmate = Chess_IsCheckmate();
    drawType = Chess_IsDraw();
    Chess_DrawAnimation(chessBoard[sPosX, sPosY]);
    if (moveInfo.isCheckmate)
    {
        if (whiteTurn)
            wLossType = 1;
        else
            bLossType = 1;
    }
    else if (drawType > 0)
        moveInfo.isDraw = true;
    if (drawType > 0 || moveInfo.isCheckmate)
    {
        for (int i = 0; i < 12; i++)
            messageFlags[i] = -1;
        messageFlags[15] = 1;
        LCDPanelsTimes[3] = endOfGameMessageTime;
        Runtime.UpdateFrequency |= UpdateFrequency.Update100;
    }
    if (gameState == 2 && (divergenceTurn == -1 || turnNumber < chessNotation.Count))
    {
        if (divergenceTurn == -1)
            divergenceTurn = turnNumber;
        for (int i = chessMoves.Count - 1; i >= turnNumber; i--)
        {
            chessMoves.RemoveAt(i);
            chessNotation.RemoveAt(i);
        }
    }
    Chess_WriteChessNotation();
    chessMoves.Add(moveInfo);
    if (gameState == 0)
    {
        time[0] = savedTime[0];
        time[1] = savedTime[1];
        increment[0] = savedIncrement[0];
        increment[1] = savedIncrement[1];
        gameState = 1;
        Runtime.UpdateFrequency |= UpdateFrequency.Update10;
    }
    else if (gameState == 1)
    {

        if (isClock[0] || isClock[1])
        {
            if (!whiteTurn && isClock[0] && isIncrement[0])
                time[0] += increment[0];
            else if (whiteTurn && isClock[1] && isIncrement[1])
                time[1] += increment[1];
        }
    }
}

public bool Chess_IsValidMove(int sourceX, int sourceY, int targetX, int targetY)
{
    ChessFigure chessPiece = chessBoard[sourceX, sourceY];
    int distX = targetX - sourceX;
    int distY = targetY - sourceY;
    if (Chess_LeavesKingInDanger(sourceX, sourceY, targetX, targetY))
        return false;

    switch (chessPiece.type)
    {
        case ChessPiece.Pawn:
            if (distX == 0 && Chess_IsPathClear(sourceX, sourceY, targetX, targetY)) 
            {
                if (chessPiece.hasMoved == -1 && (chessPiece.color ? distY == 2 : distY == -2))
                    return true;
                if (chessPiece.color ? distY == 1 : distY == -1)
                    return true;
            }
            else if ((chessPiece.color ? distY == 1 : distY == -1) && Math.Abs(distX) == 1) 
            {
                if (chessBoard[sourceX + distX, sourceY + distY] != null && chessBoard[sourceX + distX, sourceY + distY].color != chessPiece.color)
                    return true;
                if (chessBoard[sourceX + distX, sourceY] != null && chessBoard[sourceX + distX, sourceY].color != chessPiece.color && (chessPiece.color ? sourceY == 4 : sourceY == 3) && chessBoard[sourceX + distX, sourceY].hasMoved == turnNumber)
                    return true;
            }
            break;
        case ChessPiece.Rook:
            if ((distX == 0 || distY == 0) && Chess_IsPathClear(sourceX, sourceY, targetX, targetY))
                return true;
            break;
        case ChessPiece.Knight:
            if (((Math.Abs(distX) == 2 && Math.Abs(distY) == 1) || (Math.Abs(distX) == 1 && Math.Abs(distY) == 2)) && (chessBoard[targetX, targetY] == null || chessBoard[targetX, targetY].color != chessPiece.color))
                return true;
            break;
        case ChessPiece.Bishop:
            if (Math.Abs(distX) == Math.Abs(distY) && Chess_IsPathClear(sourceX, sourceY, targetX, targetY))
                return true;
            break;
        case ChessPiece.Queen:
            if ((Math.Abs(distX) == Math.Abs(distY) || distX == 0 || distY == 0) && Chess_IsPathClear(sourceX, sourceY, targetX, targetY))
                return true;
            break;
        case ChessPiece.King:
            if ((Math.Max(Math.Abs(distX), Math.Abs(distY)) <= 1 || (Math.Abs(distX) == 2 && distY == 0 && chessPiece.hasMoved == -1 && Chess_IsCastlingValid(chessPiece, distX))) && Chess_IsPathClear(sourceX, sourceY, targetX, targetY))
                return true;
            break;
    }
    return false;
}

public bool Chess_LeavesKingInDanger(int sourceX, int sourceY, int targetX, int targetY)
{
    ChessFigure chessPiece = chessBoard[sourceX, sourceY];
    ChessFigure saveChessPiece = chessBoard[targetX, targetY];
    int[] kingPos = chessPiece.color ? wKingPos.ToArray() : bKingPos.ToArray();
    
    chessBoard[targetX, targetY] = chessPiece;
    chessBoard[sourceX, sourceY] = null;
    if (chessPiece.type == ChessPiece.King ? Chess_CanMoveToCount(!chessPiece.color, targetX, targetY, true) == 0 : Chess_CanMoveToCount(!chessPiece.color, kingPos[0], kingPos[1], true) == 0)
    {
        chessBoard[targetX, targetY] = saveChessPiece;
        chessBoard[sourceX, sourceY] = chessPiece;
        return false;
    }
    else
    {
        chessBoard[targetX, targetY] = saveChessPiece;
        chessBoard[sourceX, sourceY] = chessPiece;
        return true;
    }
}

public int Chess_CanMoveToCount(bool color, int posX, int posY, bool searchEnemies)
{
    int[] dirsX = { -1, -1, 1, 1, -1, 1, 0, 0, -2, -2, 2, 2, -1, -1, 1, 1 };
    int[] dirsY = { -1, 1, -1, 1, 0, 0, -1, 1, -1, 1, -1, 1, -2, 2, -2, 2 };
    int checkX, checkY, iteration;
    int counter = 0;

    for (int i = 0; i < 16; i++)
    {
        iteration = 0;
        checkX = posX + dirsX[i];
        checkY = posY + dirsY[i];

        while (Chess_IsWithinBounds(checkX, checkY))
        {
            ChessFigure chessPiece = chessBoard[checkX, checkY];
            if (chessPiece != null && chessPiece.color != color)
                break;
            else if (chessPiece != null)
            {
                if ((chessPiece.type == ChessPiece.Pawn && (searchEnemies ? (color ? (i == 0 || i == 2) : (i == 1 || i == 3)) && iteration == 0 : Chess_IsValidMove(checkX, checkY, posX, posY))) ||
                    (chessPiece.type == ChessPiece.Knight && i >= 8 && i < 16 && iteration == 0) ||
                    (chessPiece.type == ChessPiece.King && i >= 0 && i < 8 && iteration == 0) ||
                    (chessPiece.type == ChessPiece.Bishop && i >= 0 && i < 4) ||
                    (chessPiece.type == ChessPiece.Rook && i >= 4 && i < 8) ||
                    (chessPiece.type == ChessPiece.Queen && i >= 0 && i < 8))
                {
                    if (color == whiteTurn && !searchEnemies) 
                    {
                        if (chessPiece.type != ChessPiece.King && !Chess_LeavesKingInDanger(checkX, checkY, posX, posY))
                            counter++;
                    }
                    else if (color != whiteTurn && searchEnemies) 
                    {
                        counter++;
                        tempChessPiecePos[0] = checkX;
                        tempChessPiecePos[1] = checkY;
                    }
                }
                checkX += 100;
            }
            checkX += dirsX[i];
            checkY += dirsY[i];
            iteration++;
        }
    }
    return counter;
}

public bool Chess_IsWithinBounds(int posX, int posY)
{
    return posX >= 0 && posX < 8 && posY >= 0 && posY < 8;
}

public bool Chess_IsPathClear(int sourceX, int sourceY, int targetX, int targetY)
{
    ChessFigure chessPiece = chessBoard[sourceX, sourceY];
    int distX = targetX - sourceX;
    int distY = targetY - sourceY;

    int dirX = Math.Sign(distX);
    int dirY = Math.Sign(distY);

    for (int i = 1; i < Math.Max(Math.Abs(distX), Math.Abs(distY)); i++)
        if (chessBoard[sourceX + i * dirX, sourceY + i * dirY] != null)
            return false;
    if (chessPiece.type == ChessPiece.Pawn && chessBoard[targetX, targetY] != null)
        return false;
    if (chessBoard[targetX, targetY] != null && chessBoard[targetX, targetY].color == chessPiece.color)
        return false;
    return true;
}

public bool Chess_IsCastlingValid(ChessFigure chessPiece, int distX)
{
    int rookDistance = distX > 0 ? 4 : -3;
    if (Chess_CanMoveToCount(!chessPiece.color, fPosX, fPosY, true) == 0 && Chess_CanMoveToCount(!chessPiece.color, fPosX + Math.Sign(distX), fPosY, true) == 0)
        if (chessBoard[fPosX + rookDistance, fPosY] != null && chessBoard[fPosX + rookDistance, fPosY].type == ChessPiece.Rook && chessBoard[fPosX + rookDistance, fPosY].hasMoved == -1)
            return true;
    return false;
}

public void Chess_MoveChessPiece(ChessFigure chessPiece)
{
    if(chessPiece.type == ChessPiece.King)
    {
        if (chessPiece.color)
        {
            wKingPos[0] = sPosX;
            wKingPos[1] = sPosY;
        }
        else
        {
            bKingPos[0] = sPosX;
            bKingPos[1] = sPosY;
        }  
    }
    if (chessPiece.type == ChessPiece.King && Math.Abs(moveInfo.targetX - moveInfo.sourceX) == 2)
    {
        chessBoard[sPosX, sPosY] = chessPiece;
        chessBoard[fPosX, fPosY] = null;
        if (moveInfo.targetX - moveInfo.sourceX > 0)
        {
            chessPiece = chessBoard[7, fPosY];
            chessBoard[4, fPosY] = chessPiece;
            chessBoard[7, fPosY] = null;
            moveInfo.isLCastling = true;
        }
        else
        {
            chessPiece = chessBoard[0, fPosY];
            chessBoard[2, sPosY] = chessPiece;
            chessBoard[0, fPosY] = null;
            moveInfo.isSCastling = true;
        }
    }
    else
    {
        if (chessBoard[sPosX, sPosY] != null || (chessPiece.type == ChessPiece.Pawn && Math.Abs(moveInfo.targetX - moveInfo.sourceX) > 0))
        {
            if (whiteTurn)
                Chess_RotationHandler("bRotation_scroll");
            else
                Chess_RotationHandler("wRotation_scroll");

            moveInfo.isCapture = true;
            if (chessBoard[sPosX, sPosY] != null)
            {
                moveInfo.capturedPiece = new ChessFigure(chessBoard[sPosX, sPosY]);
            }
            else
            {
                moveInfo.isEnPassant = true;
                moveInfo.capturedPiece = new ChessFigure(chessBoard[sPosX, fPosY == 3 ? sPosY + 1 : sPosY - 1]);
            }
        }
        chessBoard[sPosX, sPosY] = chessPiece;
        chessBoard[fPosX, fPosY] = null;
    } 
}

public bool Chess_IsCheckmate()
{
    int[] kingPos = whiteTurn ? wKingPos.ToArray() : bKingPos.ToArray();

    int currentAttacks = Chess_CanMoveToCount(!whiteTurn, kingPos[0], kingPos[1], true);
    if (currentAttacks == 0)
        return false;

    int[] savedtempChessPiecePos = tempChessPiecePos.ToArray();
    int[] dirsX = { -1, -1, 1, 1, -1, 1, 0, 0 };
    int[] dirsY = { -1, 1, -1, 1, 0, 0, -1, 1 };
    int checkX, checkY;
    moveInfo.isCheck = true;

    for (int i = 0; i < 8; i++)
    {
        checkX = kingPos[0] + dirsX[i];
        checkY = kingPos[1] + dirsY[i];
        if (Chess_IsWithinBounds(checkX, checkY) && (chessBoard[checkX, checkY] == null || chessBoard[checkX, checkY].color == !whiteTurn))
            if (!Chess_LeavesKingInDanger(kingPos[0], kingPos[1], checkX, checkY))
                return false;
    }
    if (currentAttacks == 1)
    {
        int dirX = Math.Sign(savedtempChessPiecePos[0] - kingPos[0]);
        int dirY = Math.Sign(savedtempChessPiecePos[1] - kingPos[1]);
        if (Math.Abs(kingPos[0] - savedtempChessPiecePos[0]) == 2 || Math.Abs(kingPos[1] - savedtempChessPiecePos[1]) == 2)
        {
            if (Chess_CanMoveToCount(whiteTurn, savedtempChessPiecePos[0], savedtempChessPiecePos[1], false) > 0)
                return false;
        }
        else
        {
            for (int i = 1; i <= Math.Max(Math.Abs(kingPos[0] - savedtempChessPiecePos[0]), Math.Abs(kingPos[1] - savedtempChessPiecePos[1])); i++)
            {
                checkX = kingPos[0] + i * dirX;
                checkY = kingPos[1] + i * dirY;
                if (Chess_CanMoveToCount(whiteTurn, checkX, checkY, false) > 0)
                {
                    return false;
                }
            }
        }
    }
    return true;
}

public int Chess_IsDraw()
{
    if (Chess_IsStalemate())
        return 1;
    fiftyMoveRuleCounter = (!moveInfo.isCapture && moveInfo.pieceType != ChessPiece.Pawn) ? fiftyMoveRuleCounter + 1 : 0;
    if (fiftyMoveRuleCounter == 100)
        return 2;
    if (Chess_IsThreefoldRepetition())
        return 3;
    if (Chess_IsInsufficientMaterial())
        return 4;
    return 0;
}

public bool Chess_IsStalemate()
{
    int[] kingPos = whiteTurn ? wKingPos : bKingPos;
    if (Chess_CanMoveToCount(!whiteTurn, kingPos[0], kingPos[1], true) > 0)
        return false;

    int[] dirsX = { -1, -1, 1, 1, -1, 1, 0, 0, -2, -2, 2, 2, -1, -1, 1, 1 };
    int[] dirsY = { -1, 1, -1, 1, 0, 0, -1, 1, -1, 1, -1, 1, -2, 2, -2, 2 };

    for (int i = 0; i < 8; i++) 
        for (int j = 0; j < 8; j++)
            if (chessBoard[i, j] != null && chessBoard[i, j].color == whiteTurn) 
                for (int k = 0; k < 16; k++)
                    if ((chessBoard[i, j].type == ChessPiece.Pawn && (whiteTurn ? (k == 1 || k == 3 || k == 7) : (k == 0 || k == 2 || k == 6))) ||
                        ((chessBoard[i, j].type == ChessPiece.Queen || chessBoard[i, j].type == ChessPiece.King) && k >= 0 && k < 8) ||
                        (chessBoard[i, j].type == ChessPiece.Knight && k >= 8 && k < 16) ||
                        (chessBoard[i, j].type == ChessPiece.Bishop && k >= 0 && k < 4) ||
                        (chessBoard[i, j].type == ChessPiece.Rook && k >= 4 && k < 8))
                        if (Chess_IsWithinBounds(i + dirsX[k], j + dirsY[k]) && Chess_IsValidMove(i, j, i + dirsX[k], j + dirsY[k]))
                            return false;
    return true;
}

public bool Chess_IsThreefoldRepetition()
{
    string lastPosition = "";
    int counter = 1;
    for (int i = 0; i < 8; i++)
        for (int j = 0; j < 8; j++)
        {
            if (chessBoard[i, j] == null)
                lastPosition += "-";
            else
            {
                lastPosition += chessBoard[i, j].color ? "w" : "b";
                lastPosition += chessBoard[i, j].type == ChessPiece.Knight ? "N" : $"{chessBoard[i, j].type.ToString()[0]}";
            }
        }
    for (int i = 0; i < positionKeys.Count; i++)
    {
        if (positionKeys[i] == lastPosition)
            counter++;
        if (counter == 3)
            return true;
    }
    positionKeys.Add(lastPosition);
    return false;
}

public bool Chess_IsInsufficientMaterial()
{
    List<ChessFigure> wPieces = new List<ChessFigure>();
    List<ChessFigure> bPieces = new List<ChessFigure>();

    for (int i = 0; i < 8; i++)
        for (int j = 0; j < 8; j++)
            if (chessBoard[i, j] != null)
                if (chessBoard[i, j].color)
                    wPieces.Add(chessBoard[i, j]);
                else
                    bPieces.Add(chessBoard[i, j]);
    if (wPieces.Count == 1 && bPieces.Count == 1)
        return true;
    if (wPieces.Count == 2 && bPieces.Count == 1)
    {
        if (wPieces[0].type == ChessPiece.Bishop || wPieces[1].type == ChessPiece.Bishop)
            return true;
        if (wPieces[0].type == ChessPiece.Knight || wPieces[1].type == ChessPiece.Knight)
            return true;
    }
    else if (wPieces.Count == 1 && bPieces.Count == 2)
    {
        if (bPieces[0].type == ChessPiece.Bishop || bPieces[1].type == ChessPiece.Bishop)
            return true;
        if (bPieces[0].type == ChessPiece.Knight || bPieces[1].type == ChessPiece.Knight)
            return true;
    }
    if (wPieces.Count == 2 && bPieces.Count == 2)
        if ((wPieces[0].type == ChessPiece.Bishop || wPieces[1].type == ChessPiece.Bishop) && (bPieces[0].type == ChessPiece.Bishop || bPieces[1].type == ChessPiece.Bishop))
        {
            int wBishopFormula = wPieces[0].type == ChessPiece.Bishop ? wPieces[0].startX + wPieces[0].startY : wPieces[1].startX + wPieces[1].startY;
            int bBishopFormula = bPieces[0].type == ChessPiece.Bishop ? bPieces[0].startX + bPieces[0].startY : bPieces[1].startX + bPieces[1].startY;
            if ((wBishopFormula % 2) == (bBishopFormula % 2))
                return true;
        }
    return false;
}

public void Chess_PromotionHandler(string arg)
{
    if (arg.EndsWith("enter"))
    {
        moveInfo.promotedTo = promotionPiece;
        chessBoard[moveInfo.targetX, moveInfo.targetY].type = promotionPiece;
        Chess_PromoteChessPiece();
        promotionPiece = ChessPiece.Queen;
        promotionFlag = false;
        Chess_PerformMoveAfterMath();
        LCD_SetPanelToDefault(true, 4);
        LCD_SetPanelToDefault(false, 4);   
        LCD_UpdateInfoPanels();
    }
    else
    {
        if (arg.EndsWith("next"))
            promotionPiece = ((int)promotionPiece + 1) == 5 ? (ChessPiece)(1) : (ChessPiece)((int)promotionPiece + 1);
        else
            promotionPiece = ((int)promotionPiece - 1) == 0 ? (ChessPiece)(4) : (ChessPiece)((int)promotionPiece - 1);

        if (arg.StartsWith("w"))
            wPanels[4].WriteText(promotionPiece.ToString());
        else
            bPanels[4].WriteText(promotionPiece.ToString());
    }
}

public void Chess_PromoteChessPiece()
{
    IMyProjector projector = chessBoard[moveInfo.targetX, moveInfo.targetY].projector;
    int i = 0, j = 0;
    switch(promotionPiece)
    {
        case ChessPiece.Queen:
            i = 1;
            j = 0;
            break;
        case ChessPiece.Rook:
            i = -1;
            j = 0;
            break;
        case ChessPiece.Knight:
            i = 0;
            j = 1;
            break;
        case ChessPiece.Bishop:
            i = 0;
            j = -1;
            break;
    }
    projector.ProjectionRotation = new Vector3I(chessBoard[moveInfo.targetX, moveInfo.targetY].projector.ProjectionRotation.X, i, j);

    projector.Enabled = false;
    projector.Enabled = true;
}

public void Chess_RotationHandler(string arg)
{
    bool color = arg.StartsWith("w") ? true : false;
    int[] rotationPiece = color ? wRotationPiece.ToArray() : bRotationPiece.ToArray();

    if (arg.EndsWith("scroll"))
    {
        if ((color && LCDPanelsTimes[0] > 0) || (!color && LCDPanelsTimes[1] > 0) || gameState == 2)
        {
            int i = rotationPiece[0], j = rotationPiece[1];
            do
            {
                j = (j + 1) % 8;
                i = j == 0 ? (i + 1) % 8 : i;
            }
            while (chessBoard[i, j] == null || chessBoard[i, j].color != color);
            Chess_UpdateRotationSelection(color, i, j);
        }
        else
            Chess_UpdateRotationSelection(color, rotationPiece[0], rotationPiece[1]);
    }
    else
    {
        IMyProjector projector = color ? chessBoard[wRotationPiece[0], wRotationPiece[1]].projector : chessBoard[bRotationPiece[0], bRotationPiece[1]].projector;
        projector.ProjectionRotation = new Vector3I(projector.ProjectionRotation.X + (arg.EndsWith("next") ? 1 : -1), projector.ProjectionRotation.Y, projector.ProjectionRotation.Z);
        Chess_UpdateRotationSelection(color, -1, -1);  
    }
}

public void Chess_UpdateRotationSelection(bool color, int posX, int posY)
{
    if (color)
    {
        if(Chess_IsWithinBounds(posX, posY))
        {
            wRotationPiece[0] = posX;
            wRotationPiece[1] = posY;
        }
        wPanels[3].WriteText($"{(char)(65 + wRotationPiece[0])}{(char)(49 + wRotationPiece[1])} {chessBoard[wRotationPiece[0], wRotationPiece[1]].type}");
        wPanels[3].FontSize = 8;
        wPanels[3].TextPadding = 5;
        LCDPanelsTimes[0] = 3;
    }
    else
    {
        if (Chess_IsWithinBounds(posX, posY))
        {
            bRotationPiece[0] = posX;
            bRotationPiece[1] = posY;
        }
        bPanels[3].WriteText($"{(char)(65 + bRotationPiece[0])}{(char)(49 + bRotationPiece[1])} {chessBoard[bRotationPiece[0], bRotationPiece[1]].type}");
        bPanels[3].FontSize = 8;
        bPanels[3].TextPadding = 5;
        LCDPanelsTimes[1] = 3;
    }
    Runtime.UpdateFrequency |= UpdateFrequency.Update100;
}

public void Chess_DrawAnimation(ChessFigure chessPiece)
{
    if (animationFlag)
    {
        animationCounter = (animationCounter + 1) % (gameState == 2 ? gameLoadSpeed : animationSpeed);
        if(animationCounter == 0)
        {
            if (isSound)
                Sound_PlayGameSounds(true);
            if (chessPiece.type == ChessPiece.Knight && moveInfo.sourceX == fPosX && moveInfo.sourceY == fPosY && !chessPiece.isOriginallyPawn && chessPiece.projector.ProjectionOffset.Y != 4 && knightsJump)
                chessPiece.projector.ProjectionOffset = new Vector3I(fPosX - chessPiece.startX, 4, fPosY - chessPiece.startY);
            else if (fPosX != sPosX || fPosY != sPosY)
            {
                if (chessPiece.type != ChessPiece.Knight && chessPiece.type != ChessPiece.King)
                {
                    fPosX += Math.Sign(sPosX - fPosX);
                    fPosY += Math.Sign(sPosY - fPosY);
                    chessPiece.projector.ProjectionOffset = new Vector3I(fPosX - chessPiece.startX, chessPiece.projector.ProjectionOffset.Y, fPosY - chessPiece.startY);
                }
                else if (chessPiece.type == ChessPiece.Knight)
                {
                    fPosX += Math.Sign(sPosX - fPosX);
                    fPosY += Math.Sign(sPosY - fPosY);
                    if (Math.Abs(moveInfo.targetX - moveInfo.sourceX) == 2)
                    {
                        if (fPosX != sPosX)
                        {
                            chessPiece.projector.ProjectionOffset = new Vector3I(fPosX - chessPiece.startX, chessPiece.projector.ProjectionOffset.Y, moveInfo.sourceY - chessPiece.startY);
                            fPosY -= ((knightsFullMove ? 1 : 0) + 1) * Math.Sign(moveInfo.targetY - moveInfo.sourceY);
                        }
                        else
                            chessPiece.projector.ProjectionOffset = new Vector3I(fPosX - chessPiece.startX, chessPiece.projector.ProjectionOffset.Y, fPosY - chessPiece.startY);
                    }
                    else if (Math.Abs(moveInfo.targetY - moveInfo.sourceY) == 2)
                    {
                        if (fPosY != sPosY)
                        {
                            chessPiece.projector.ProjectionOffset = new Vector3I(moveInfo.sourceX - chessPiece.startX, chessPiece.projector.ProjectionOffset.Y, fPosY - chessPiece.startY);
                            fPosX -= ((knightsFullMove ? 1 : 0) + 1) * Math.Sign(moveInfo.targetX - moveInfo.sourceX);
                        }
                        else
                            chessPiece.projector.ProjectionOffset = new Vector3I(fPosX - chessPiece.startX, chessPiece.projector.ProjectionOffset.Y, fPosY - chessPiece.startY);
                    }
                }
                else
                {
                    chessPiece = (moveInfo.targetX - moveInfo.sourceX > 0) ? chessBoard[4, fPosY] : chessBoard[2, fPosY];
                    if ((moveInfo.targetX - moveInfo.sourceX > 0) ? chessPiece.projector.ProjectionOffset.X != -3 : chessPiece.projector.ProjectionOffset.X != 2)
                    {
                        if (moveInfo.targetX - moveInfo.sourceX > 0)
                            chessPiece.projector.ProjectionOffset = new Vector3I(chessPiece.projector.ProjectionOffset.X - 1, 3, 0);
                        else
                            chessPiece.projector.ProjectionOffset = new Vector3I(chessPiece.projector.ProjectionOffset.X + 1, 3, 0);
                    }
                    else
                    {
                        fPosX += Math.Sign(sPosX - fPosX);
                        fPosY += Math.Sign(sPosY - fPosY);
                        chessPiece = (moveInfo.targetX - moveInfo.sourceX > 0) ? chessBoard[5, fPosY] : chessBoard[1, fPosY];
                        chessPiece.projector.ProjectionOffset = new Vector3I(fPosX - chessPiece.startX, 3, fPosY - chessPiece.startY);
                    }
                }
            }
            else
            {
                if (moveInfo.isCapture && chessPiece.projector.ProjectionOffset.Y != 4)
                    moveInfo.capturedPiece.projector.Enabled = false;
                else if (knightsJump && chessPiece.projector.ProjectionOffset.Y == 4)
                {
                    if (moveInfo.isCapture)
                        moveInfo.capturedPiece.projector.Enabled = false;
                    chessPiece.projector.ProjectionOffset = new Vector3I(sPosX - chessPiece.startX, 3, sPosY - chessPiece.startY);
                }
                if (!loadGameFlag)
                    Runtime.UpdateFrequency &= ~UpdateFrequency.Update1;
                animationFlag = false;
                if (isSound)
                    Sound_PlayGameSounds(true);
                if (messageFlags[15] > 0 && isMusic)
                    Sound_PlayMusic(true);
                fPosX = fPosY = sPosX = sPosY = -1;
            }
        }
    }
    else
    {
        if (moveInfo.isSCastling || moveInfo.isLCastling)
        {
            chessPiece.projector.ProjectionOffset = new Vector3I(moveInfo.targetX - chessPiece.startX, 3, moveInfo.targetY - chessPiece.startY);
            if (moveInfo.targetX - moveInfo.sourceX > 0)
            {
                chessPiece = chessBoard[4, moveInfo.targetY];
                chessPiece.projector.ProjectionOffset = new Vector3I(-3, 3, 0);
            }
            else
            {
                chessPiece = chessBoard[2, moveInfo.targetY];
                chessPiece.projector.ProjectionOffset = new Vector3I(2, 3, 0);
            }
        }
        else
        {
            if (moveInfo.isCapture)
                moveInfo.capturedPiece.projector.Enabled = false;          
            chessPiece.projector.ProjectionOffset = new Vector3I(sPosX - chessPiece.startX, chessPiece.projector.ProjectionOffset.Y, sPosY - chessPiece.startY);
        }
        fPosX = fPosY = sPosX = sPosY = -1;
        if (isSound)
            Sound_PlayGameSounds(true);
    }
}

public void Chess_WriteChessNotation()
{
    string lastMove;
    string sourceSquare = $"{(char)(97 + moveInfo.sourceX)}{(char)(49 + moveInfo.sourceY)}";
    string targetSquare = $"{(char)(97 + moveInfo.targetX)}{(char)(49 + moveInfo.targetY)}";

    if (moveInfo.isSCastling || moveInfo.isLCastling)
        lastMove = moveInfo.isSCastling ? "0-0" : "0-0-0";
    else if (moveInfo.pieceType == ChessPiece.Pawn)
        lastMove = $"{(moveInfo.isCapture ? sourceSquare[0] + 'x'.ToString() : "")}{targetSquare[0]}{targetSquare[1]}{(moveInfo.isPromotion ? '='.ToString() + moveInfo.promotedTo.ToString()[0] : "")}";
    else
    {
        if (moveInfo.pieceType == ChessPiece.Knight)
            lastMove = "N";
        else
            lastMove = $"{moveInfo.pieceType.ToString()[0]}";
        if (Chess_IsNotationDiscrepancy(moveInfo.sourceX, moveInfo.sourceY, moveInfo.targetX, moveInfo.targetY, true) || Chess_IsNotationDiscrepancy(moveInfo.sourceX, moveInfo.sourceY, moveInfo.targetX, moveInfo.targetY, false))
        {
            if (moveInfo.pieceType == ChessPiece.Queen && Chess_IsNotationDiscrepancy(moveInfo.sourceX, moveInfo.sourceY, moveInfo.targetX, moveInfo.targetY, false)
                && Chess_IsNotationDiscrepancy(moveInfo.sourceX, moveInfo.sourceY, moveInfo.targetX, moveInfo.targetY, true))
                lastMove += $"{sourceSquare[0]}{sourceSquare[1]}";
            else if (!Chess_IsNotationDiscrepancy(moveInfo.sourceX, moveInfo.sourceY, moveInfo.targetX, moveInfo.targetY, false))
                lastMove += $"{sourceSquare[0]}";
            else
                lastMove += $"{sourceSquare[1]}";
        }
        if (moveInfo.isCapture)
            lastMove += "x";
        lastMove += $"{targetSquare[0]}{targetSquare[1]}";
    }
    if (moveInfo.isCheckmate)
    {
        lastMove += "#";
    }
    else if (moveInfo.isCheck)
    {
        lastMove += "+";
    }
    chessNotation.Add(lastMove);
}

public bool Chess_IsNotationDiscrepancy(int sourceX, int sourceY, int targetX, int targetY, bool scanHorizontal)
{
    int checkX, checkY;
    ChessFigure saveChessPiece = chessBoard[targetX, targetY];
    chessBoard[sourceX, sourceY] = saveChessPiece;
    chessBoard[targetX, targetY] = null;

    for (int i = 0; i < 8; i++)
    {
        checkX = scanHorizontal ? i : sourceX;
        checkY = !scanHorizontal ? i : sourceY;
        if (!(checkX == sourceX && checkY == sourceY) && chessBoard[checkX, checkY] != null && chessBoard[checkX, checkY].color == chessBoard[sourceX, sourceY].color && chessBoard[checkX, checkY].type == moveInfo.pieceType)
            if (Chess_IsValidMove(checkX, checkY, targetX, targetY))
            {
                chessBoard[sourceX, sourceY] = null;
                chessBoard[targetX, targetY] = saveChessPiece;
                return true;
            }
    }
    chessBoard[sourceX, sourceY] = null;
    chessBoard[targetX, targetY] = saveChessPiece;
    return false;
}

public void Chess_InitializeGame()
{
    IMyProjector projector;
    bool isWhite;
    for (int i = 0; i < 8; i++)
    {
        for (int j = 0; j < 8; j++)
        {
            if (j < 2) projector = projectors[8 * j + i];
            else if (j > 5) projector = projectors[8 * (j - 4) + i];
            else projector = null;
            isWhite = j < 2;
            ChessPiece pieceType = ChessPiece.Pawn;

            if (projector != null)
            {
                if (j == 1 || j == 6)
                {
                    if(projector.ProjectionRotation.Y != 2 || projector.ProjectionRotation.Z != 0)
                    {
                        projector.ProjectionRotation = new Vector3I(projector.ProjectionRotation.X, 2, 0);
                        projector.Enabled = false;
                        projector.Enabled = true;
                    }
                    else
                        projector.ProjectionRotation = new Vector3I(projector.ProjectionRotation.X, 2, 0);
                    projector.ProjectionOffset = new Vector3I(0, 1, 0);
                }
                else if (j == 0 || j == 7)
                {
                    switch (i)
                    {
                        case 0:
                        case 7:
                            pieceType = ChessPiece.Rook;
                            break;
                        case 1:
                        case 6:
                            pieceType = ChessPiece.Knight;
                            break;
                        case 2:
                        case 5:
                            pieceType = ChessPiece.Bishop;
                            break;
                        case 4:
                            pieceType = ChessPiece.Queen;
                            break;
                        case 3:
                            pieceType = ChessPiece.King;
                            break;
                    }
                    projector.ProjectionOffset = new Vector3I(0, 3, 0);
                }
                chessBoard[i, j] = new ChessFigure(projector, i, j, pieceType, isWhite);
                projector.Enabled = true;
            }
            else
            {
                chessBoard[i, j] = null;
            }
        }
    }
    for (int i = 0; i < 16; i++)
        messageFlags[i] = -1;
    for (int i = 0; i < 4; i++)
        LCDPanelsTimes[i] = 0;
    turnNumber = -1;
    whiteTurn = true;
    promotionFlag = false;
    wKingPos[0] = bKingPos[0] = 3;
    wKingPos[1] = wRotationPiece[0] = wRotationPiece[1] = bRotationPiece[0] = 0;
    bKingPos[1] = bRotationPiece[1] = 7;
    chessNotation.Clear();
    chessMoves.Clear();
    positionKeys.Clear();
    fiftyMoveRuleCounter = 0;
    time[0] = savedTime[0];
    time[1] = savedTime[1];
    increment[0] = savedIncrement[0];
    increment[1] = savedIncrement[1];
    drawType = wLossType = bLossType = 0;
}

public void Chess_LoadMove()
{
    turnNumber++;
    moveInfo = chessMoves[turnNumber];
    fPosX = moveInfo.sourceX;
    fPosY = moveInfo.sourceY;
    sPosX = moveInfo.targetX;
    sPosY = moveInfo.targetY;
    ChessFigure chessPiece = chessBoard[moveInfo.sourceX, moveInfo.sourceY];
    chessPiece.hasMoved = turnNumber;
    Chess_MoveChessPiece(chessPiece);
    if (animationSpeed > 0 && Math.Max(Math.Abs(moveInfo.targetX - moveInfo.sourceX), Math.Abs(moveInfo.targetY - moveInfo.sourceY)) > 1)
    {
        animationFlag = true;
        Runtime.UpdateFrequency |= UpdateFrequency.Update1;
    }
    Chess_DrawAnimation(chessPiece);
    if (moveInfo.isPromotion)
    {
        promotionPiece = moveInfo.promotedTo;
        Chess_PromoteChessPiece();
    }
    whiteTurn = !whiteTurn;
    moveInfo.isCheckmate = Chess_IsCheckmate();
    if (moveInfo.isCheckmate)
        if (whiteTurn)
            wLossType = 1;
        else
            bLossType = 1;
    drawType = Chess_IsDraw();
    if (drawType > 0)
        moveInfo.isDraw = true;
}

public void Chess_TakeBack()
{
    moveInfo = chessMoves[turnNumber];
    chessBoard[moveInfo.sourceX, moveInfo.sourceY] = chessBoard[moveInfo.targetX, moveInfo.targetY];
    chessBoard[moveInfo.targetX, moveInfo.targetY] = null;
    ChessFigure chessPiece = chessBoard[moveInfo.sourceX, moveInfo.sourceY];
    chessPiece.projector.ProjectionOffset = new Vector3I(moveInfo.sourceX - chessPiece.startX, chessPiece.isOriginallyPawn ? 1 : 3, moveInfo.sourceY - chessPiece.startY);
    if (chessPiece.hasMoved == turnNumber)
        chessPiece.hasMoved = -1;
    if (moveInfo.isPromotion)
    {
        chessPiece.type = ChessPiece.Pawn;
        chessPiece.projector.ProjectionRotation = new Vector3I(chessPiece.projector.ProjectionRotation.X, 2, 0);
        chessPiece.projector.Enabled = false;
        chessPiece.projector.Enabled = true;
    }
    if (moveInfo.isEnPassant)
    {
        chessBoard[moveInfo.targetX, moveInfo.targetY == 5 ? moveInfo.targetY - 1 : moveInfo.targetY + 1] = new ChessFigure(moveInfo.capturedPiece);
        moveInfo.capturedPiece.projector.Enabled = true;
    }
    else if (moveInfo.isCapture)
    {
        chessBoard[moveInfo.targetX, moveInfo.targetY] = new ChessFigure(moveInfo.capturedPiece);
        moveInfo.capturedPiece.projector.Enabled = true;
    }

    if (moveInfo.isSCastling)
    {
        chessBoard[0, moveInfo.sourceY] = chessBoard[2, moveInfo.sourceY];
        chessBoard[2, moveInfo.sourceY] = null;
        chessBoard[0, moveInfo.sourceY].projector.ProjectionOffset = new Vector3I(0, 3, 0);
    }
    else if (moveInfo.isLCastling)
    {
        chessBoard[7, moveInfo.sourceY] = chessBoard[4, moveInfo.sourceY];
        chessBoard[4, moveInfo.sourceY] = null;
        chessBoard[7, moveInfo.sourceY].projector.ProjectionOffset = new Vector3I(0, 3, 0);
    }
    if (chessPiece.type == ChessPiece.King)
    {
        if (!whiteTurn)
        {
            wKingPos[0] = moveInfo.sourceX;
            wKingPos[1] = moveInfo.sourceY;
        }
        else
        {
            bKingPos[0] = moveInfo.sourceX;
            bKingPos[1] = moveInfo.sourceY;
        }
    }
    fiftyMoveRuleCounter = (!moveInfo.isCapture && moveInfo.pieceType != ChessPiece.Pawn) ? fiftyMoveRuleCounter - 1 : 0;
    whiteTurn = !whiteTurn;
    positionKeys.RemoveAt(positionKeys.Count - 1);
    if (turnNumber == divergenceTurn)
    {
        if (menuChoice > 2)
        {
            chessNotation = chessNotationsList[menuChoice - 3].ToList();
            chessMoves = chessMovesList[menuChoice - 3].ToList();
        }
        else
        {
            chessNotation = chessNotationsList[chessNotationsList.Count - 1].ToList();
            chessMoves = chessMovesList[chessMovesList.Count - 1].ToList();
        }
        divergenceTurn = -1;
    }
    turnNumber--;
    promotionFlag = false;
    promotionPiece = ChessPiece.Queen;
    drawType = wLossType = bLossType = 0;
    Sound_PlayGameSounds(true);
}

public void Input_ClockSettingsHandler(string arg)
{
    int isRequestFlag = -1;
    for (int i = 0; i < 12; i++)
        if (messageFlags[i] >= 0)
            isRequestFlag = i;
    if (gameState == 0)
    {
        int i = arg.StartsWith("w") ? 0 : 1;
        switch (arg)
        {
            case "wTime_onOff":
                isClock[0] = !isClock[0];
                break;
            case "bTime_onOff":
                isClock[1] = !isClock[1];
                break;
            case "wIncrement_onOff":
                isIncrement[0] = !isIncrement[0];
                break;
            case "bIncrement_onOff":
                isIncrement[1] = !isIncrement[1];
                break;
            case "wTime_increase":
            case "bTime_increase":
            case "wTime_decrease":
            case "bTime_decrease":
                if (isClock[i])
                    if (arg.EndsWith("increase") && savedTime[i] < 3600)
                        savedTime[i] += 60;
                    else if (arg.EndsWith("decrease") && savedTime[i] > 60)
                        savedTime[i] -= 60;
                break;
            case "wIncrement_increase":
            case "bIncrement_increase":
            case "wIncrement_decrease":
            case "bIncrement_decrease":
                if (isIncrement[i])
                    if (arg.EndsWith("increase") && savedIncrement[i] < 60)
                        savedIncrement[i]++;
                    else if (arg.EndsWith("decrease") && savedIncrement[i] > 0)
                        savedIncrement[i]--;
                break;
        }
    }
    else
    {
        bool validRequest = false;
        if (isRequestFlag == -1 && arg.EndsWith("onOff"))
        {
            int i = 0;
            switch (arg)
            {
                case "wTime_onOff":
                    i = 4;
                    break;
                case "bTime_onOff":
                    i = 5;
                    break;
                case "wIncrement_onOff":
                    i = 6;
                    break;
                case "bIncrement_onOff":
                    i = 7;
                    break;
            }
            validRequest = true;
            messageFlags[i] = turnNumber;
        }
        else if (((arg.StartsWith("wTime") || (arg.StartsWith("wIncrement") && isIncrement[0])) && (isRequestFlag == -1 || (arg.StartsWith("wTime") ? timeIncrease[0] != 0 : incrementIncrease[0] != 0)) && isClock[0]) ||
            ((arg.StartsWith("bTime") || (arg.StartsWith("bIncrement") && isIncrement[1])) && (isRequestFlag == -1 || (arg.StartsWith("bTime") ? timeIncrease[1] != 0 : incrementIncrease[1] != 0)) && isClock[1]))
        {
            int i = arg.StartsWith("w") ? 0 : 1;
            if (arg.StartsWith("wTime") || arg.StartsWith("bTime"))
            {
                if (arg.EndsWith("increase") && time[i] + timeIncrease[i] < 3600)
                {
                    if (timeIncrease[i] + 60 == 0)
                        timeIncrease[i] += 120;
                    else
                        timeIncrease[i] += 60; 
                }
                else if (arg.EndsWith("decrease") && time[i] + timeIncrease[i] > 90)
                {
                    if (timeIncrease[i] - 60 == 0)
                        timeIncrease[i] -= 120;
                    else
                        timeIncrease[i] -= 60;
                }
                messageFlags[8 + i] = turnNumber;
            }
            else
            {
                if (arg.EndsWith("increase") && increment[i] + incrementIncrease[i] < 60)
                {
                    if (incrementIncrease[i] + 1 == 0)
                        incrementIncrease[i] += 2;
                    else
                        incrementIncrease[i]++;
                }
                else if (arg.EndsWith("decrease") && increment[i] + incrementIncrease[i] > 0)
                {
                    if (incrementIncrease[i] - 1 == 0)
                        incrementIncrease[i] -= 2;
                    else
                        incrementIncrease[i]--;
                }
                messageFlags[10 + i] = turnNumber;
            }
            if (isRequestFlag == -1)
                validRequest = true;
        }
        if (validRequest && isNotificationSound)
        {
            foreach (IMySoundBlock block in soundBlocks)
            {
                block.SelectedSound = notificationSound.SoundName;
                block.Volume = notificationSound.Volume;
                block.Play();
            }
            Sound_PlayGameSounds(false);
        }
        LCDPanelsTimes[3] = requestMessageTime;
        Runtime.UpdateFrequency |= UpdateFrequency.Update100;
    }
    if (gameState == 0)
    {
        wPanels[2].FontSize = bPanels[2].FontSize = wPanels[1].FontSize = bPanels[1].FontSize = 9;
        wPanels[2].TextPadding = bPanels[2].TextPadding = wPanels[1].TextPadding = bPanels[1].TextPadding = 1.5f;
        if (isClock[0])
        {
            wPanels[2].WriteText($"{(savedTime[0] / 60).ToString()}:00");
            if (isIncrement[0] && savedIncrement[0] > 0)
                wPanels[1].WriteText($"+{savedIncrement[0].ToString()}");
            else
                wPanels[1].WriteText("None");
        }
        else
        {
            wPanels[2].WriteText("Unlimited");
            wPanels[1].WriteText("None");
        }
        if (isClock[1])
        {
            bPanels[2].WriteText($"{(savedTime[1] / 60).ToString()}:00");
            if (isIncrement[1] && savedIncrement[1] > 0)
                bPanels[1].WriteText($"+{savedIncrement[1].ToString()}");
            else
                bPanels[1].WriteText("None");
        }
        else
        {
            bPanels[2].WriteText("Unlimited");
            bPanels[1].WriteText("None");
        }
        LCDPanelsTimes[2] = 3;
        Runtime.UpdateFrequency |= UpdateFrequency.Update100;
    }
    LCD_UpdateInfoPanels();
}

public void Input_SpecialButtonsHandler(string arg)
{
    int isRequestFlag = -1;
    for (int i = 0; i < 12; i++)
        if (messageFlags[i] >= 0)
            isRequestFlag = i;
    bool greenButtonPress = arg == "wGreenButton" || arg == "bGreenButton";
    bool orangeButtonPress = arg == "wOrangeButton" || arg == "bOrangeButton";
    bool redButtonPress = arg == "wRedButton" || arg == "bRedButton";
    if (gameState == 1)
    {
        if ((arg == "wRedButton" && (isRequestFlag == -1 || isRequestFlag % 2 == 0)) || (arg == "bRedButton" && (isRequestFlag == -1 || isRequestFlag % 2 == 1)))
        {
            if (arg == "wRedButton")
            {
                wLossType = 2;
                TB_White.StartCountdown();
            }
            else
            {
                bLossType = 2;
                TB_White.StartCountdown();
            }
            for (int i = 0; i < 12; i++)
                messageFlags[i] = -1;
            messageFlags[15] = 1;
            LCDPanelsTimes[3] = endOfGameMessageTime;
        }
        else if (isRequestFlag >= 0)
        {
            switch (isRequestFlag)
            {
                case 0:
                case 1:
                    if ((arg == "bGreenButton" && isRequestFlag == 0 && turnNumber == messageFlags[0]) || (arg == "wGreenButton" && isRequestFlag == 1 && turnNumber == messageFlags[1]))
                    {
                        Chess_TakeBack();
                        if (turnNumber == -1)
                            gameState = 0;
                        chessNotation.RemoveAt(chessNotation.Count - 1);
                        chessMoves.RemoveAt(chessMoves.Count - 1);
                    }
                    break;
                case 2:
                    if (arg == "bGreenButton")
                        drawType = 5;
                    break;
                case 3:
                    if (arg == "wGreenButton")
                        drawType = 5;
                    break;
                case 4:
                    if (arg == "bGreenButton")
                        isClock[0] = !isClock[0];
                    break;
                case 5:
                    if (arg == "wGreenButton")
                        isClock[1] = !isClock[1];
                    break;
                case 6:
                    if (arg == "bGreenButton")
                        isIncrement[0] = !isIncrement[0];
                    break;
                case 7:
                    if (arg == "wGreenButton")
                        isIncrement[1] = !isIncrement[1];
                    break;
                case 8:
                    if (arg == "bGreenButton" || redButtonPress)
                    {
                        if (arg == "bGreenButton" && time[0] + timeIncrease[0] > 60)
                            time[0] += timeIncrease[0];
                        timeIncrease[0] = 0;
                    }
                    break;
                case 9:
                    if (arg == "wGreenButton" || redButtonPress)
                    {
                        if (arg == "wGreenButton" && time[1] + timeIncrease[1] > 60)
                            time[1] += timeIncrease[1];
                        timeIncrease[1] = 0;
                    }
                    break;
                case 10:
                    if (arg == "bGreenButton" || redButtonPress)
                    {
                        if (arg == "bGreenButton")
                            increment[0] += incrementIncrease[0];
                        incrementIncrease[0] = 0;
                    }
                    break;
                case 11:
                    if (arg == "wGreenButton" || redButtonPress)
                    {
                        if (arg == "wGreenButton")
                            increment[1] += incrementIncrease[1];
                        incrementIncrease[1] = 0;
                    }
                    break;
            }
            if ((arg == "bGreenButton" && isRequestFlag % 2 == 0) || (arg == "wGreenButton" && isRequestFlag % 2 == 1) || redButtonPress)
            {
                messageFlags[isRequestFlag] = -1;
                LCDPanelsTimes[3] = 0;
                if(drawType > 0)
                {
                    messageFlags[15] = 1;
                    LCDPanelsTimes[3] = endOfGameMessageTime;
                }
            }
        }
        else
        {
            bool validRequest = true;
            switch (arg)
            {
                case "wGreenButton":
                    if (turnNumber % 2 == 0 && !promotionFlag)
                        messageFlags[0] = turnNumber;
                    else
                        validRequest = false;
                    break;
                case "bGreenButton":
                    if (turnNumber % 2 == 1 && !promotionFlag)
                        messageFlags[1] = turnNumber;
                    else
                        validRequest = false;
                    break;
                case "wOrangeButton":
                    messageFlags[2] = turnNumber;
                    break;
                case "bOrangeButton":
                    messageFlags[3] = turnNumber;
                    break;
            }
            if(validRequest)
            {
                if (isNotificationSound)
                {
                    foreach (IMySoundBlock block in soundBlocks)
                    {
                        block.SelectedSound = notificationSound.SoundName;
                        block.Volume = notificationSound.Volume;
                        block.Play();
                    }
                    Sound_PlayGameSounds(false);
                }
                LCDPanelsTimes[3] = requestMessageTime;
            }
                
        }
        Runtime.UpdateFrequency |= UpdateFrequency.Update100;
    }
    else if (gameState == 2)
    {
        if(additionalChoiceFlag)
        {
            if (greenButtonPress || redButtonPress)
            {
                if (greenButtonPress)
                {
                    chessMovesList.Add(chessMoves.ToList());
                    chessNotationsList.Add(chessNotation.ToList());
                    enterNameFlag = true;
                }
                menuChoice = 0;
                gameState = 0;
                divergenceTurn = -1;
            }
            additionalChoiceFlag = false;
        }
        else
        {
            if (greenButtonPress)
            {
                if (turnNumber >= 0)
                    Chess_TakeBack();
            }
            else if (orangeButtonPress)
            {
                if (drawType == 0 && wLossType == 0 && bLossType == 0 && turnNumber != chessNotation.Count - 1)
                    Chess_LoadMove();
            }
            else if (redButtonPress)
            {
                if (chessNotationsList.Count > chessGamesNameList.Count)
                {
                    chessNotationsList.RemoveAt(chessNotationsList.Count - 1);
                    chessMovesList.RemoveAt(chessMovesList.Count - 1);
                }
                if (divergenceTurn != -1 && chessNotation.Count - divergenceTurn > minTurnToSave && !chessNotationsList.Any(list => list.SequenceEqual(chessNotation.ToList())))
                    additionalChoiceFlag = true;
                else
                {
                    menuChoice = 0;
                    gameState = 0;
                }
            }
        }
    }
    else if (gameState == 3)
    {
        if (additionalChoiceFlag)
        {
            if (greenButtonPress)
            {
                Runtime.UpdateFrequency |= UpdateFrequency.Update1;
                loadGameFlag = true;
                Chess_InitializeGame();
                chessNotation = chessNotationsList[menuChoice - 3].ToList();
                chessMoves = chessMovesList[menuChoice - 3].ToList();
                gameState = 2;
            }
            else if (redButtonPress)
            {
                chessGamesNameList.RemoveAt(menuChoice - 3);
                chessNotationsList.RemoveAt(menuChoice - 3);
                chessMovesList.RemoveAt(menuChoice - 3);
                menuChoice = 0;
            }
            additionalChoiceFlag = false;
        }
        else
        {
            if (greenButtonPress)
            {
                if (menuChoice == 0)
                    gameState = 0;
                else if (menuChoice == 1)
                {
                    if (!chessNotationsList.Any(list => list.SequenceEqual(chessNotation.ToList())) && chessNotation.Count > minTurnToSave)
                    {
                        chessMovesList.Add(chessMoves.ToList());
                        chessNotationsList.Add(chessNotation.ToList());
                        enterNameFlag = true;
                    }
                    else
                    {
                        Runtime.UpdateFrequency |= UpdateFrequency.Update100;
                        if (chessNotation.Count > 5)
                            messageFlags[13] = 1;
                        else
                            messageFlags[12] = 1;
                        LCDPanelsTimes[3] = errorMessageTime;
                    }
                }
                else if (menuChoice == 2)
                {
                    chessNotationsList.Clear();
                    chessMovesList.Clear();
                    chessGamesNameList.Clear();
                }
                else if (menuChoice < chessNotationsList.Count + 3)
                {
                    additionalChoiceFlag = true;
                }
            }
            else if (orangeButtonPress)
            {
                if (menuChoice < 1)
                    menuChoice = chessNotationsList.Count + 2;
                else
                    menuChoice--;
            }
            else if (redButtonPress)
            {
                if (menuChoice > chessNotationsList.Count + 1)
                    menuChoice = 0;
                else
                    menuChoice++;
            }
        }
    }
    else if (chessNotation.Count > 0 || chessNotationsList.Count > 0)
    {
        if (greenButtonPress)
            Chess_InitializeGame();
        else if (orangeButtonPress)
            gameState = 3; 
        else if (redButtonPress)
        {
            Runtime.UpdateFrequency |= UpdateFrequency.Update1;
            loadGameFlag = true;
            chessNotationsList.Add(chessNotation.ToList());
            chessMovesList.Add(chessMoves.ToList());
            Chess_InitializeGame();
            chessNotation = chessNotationsList[chessNotationsList.Count - 1].ToList();
            chessMoves = chessMovesList[chessMovesList.Count - 1].ToList();
            gameState = 2;
        }
    }
    LCD_UpdateInfoPanels();
}

public void LCD_UpdateInfoPanels()
{
    string[] asciiNumbers = new string[]
    {
            "   ████    ",
            " ██     ██ ",
            "██       ██",
            "██       ██",
            "██       ██",
            " ██     ██ ",
            "   ████    ",
            "     ██      ",
            "   █  █      ",
            "████      ",
            "     ██      ",
            "     ██      ",
            "     ██      ",
            "██████ ",
            "   ████    ",
            "██      ██ ",
            "          ██  ",
            "    ████   ",
            " ██           ",
            "██            ",
            "██████  ",
            "   ████    ",
            "██      ██ ",
            "           ██ ",
            "   ████    ",
            "           ██ ",
            "██      ██ ",
            "   ████    ",
            "     ███   ",
            "   █  ██   ",
            "  █   ██   ",
            " █    ██   ",
            "██████ ",
            "       ██    ",
            "     ████ ",
            "██████  ",
            "██            ",
            "██            ",
            "██████  ",
            "           ██ ",
            "██      ██ ",
            "   ████    ",
            "    ████   ",
            " ██      ██",
            " ██           ",
            "██████  ",
            " ██      ██",
            " ██      ██",
            "    ████   ",
            "██████  ",
            "          ██  ",
            "          ██  ",
            "       ██     ",
            "      ██      ",
            "     ██       ",
            "     ██       ",
            "   ████    ",
            "██      ██ ",
            "██      ██ ",
            "   ████    ",
            "██      ██ ",
            "██      ██ ",
            "   ████    ",
            "   ████    ",
            "██      ██ ",
            "██      ██ ",
            "  ██████",
            "           ██ ",
            "██      ██ ",
            "   ████    ",
            "███         ██ ",
            "████      ██ ",
            "██  ██    ██ ",
            "██   ██   ██  ",
            "██    ██  ██ ",
            "██      ████ ",
            "██         ███ ",
            "         ██ ",
            "       ████ ",
            "    ██      ██ ",
            "  ██        ██ ",
            "  ████████",
            " ██            ██ ",
            "██             ██ "
    };

    string wString = "", bString = "", tempString = "";
    if (messageFlags[12] >= 0 || messageFlags[13] >= 0 || messageFlags[14] >= 0 || messageFlags[15] >= 0 || enterNameFlag || additionalChoiceFlag)
    {
        if (messageFlags[12] >= 0 || messageFlags[13] >= 0 || messageFlags[14] >= 0 || messageFlags[15] >= 0)
        {
            for (int i = 12; i < 16; i++)
            {
                if (messageFlags[i] > 0)
                {
                    switch (i)
                    {
                        case 12:
                            tempString = $"                Game too short to save! \n\n             (At least {minTurnToSave} moves required!)";
                            break;
                        case 13:
                            tempString = "\n                 Game already saved!\n";
                            break;
                        case 14:
                            tempString = "\n            This name is already taken!\n";
                            break;
                        case 15:
                            if (!animationFlag && isMusic)
                                Sound_PlayMusic(true);
                            if (wLossType > 0)
                            {
                                TB_White.StartCountdown();
                                switch (wLossType)
                                {
                                    case 1:
                                        tempString = "                          Checkmate! \n\n                          Black wins!";
                                        break;
                                    case 2:
                                        tempString = "                      White resigned! \n\n                         Black wins!";
                                        break;
                                    case 3:
                                        tempString = "                  White ran out of time! \n\n                         Black wins!";
                                        break;
                                }
                            }
                            else if (bLossType > 0)
                            {
                                TB_Black.StartCountdown();
                                switch (bLossType)
                                {
                                    case 1:
                                        tempString = "                          Checkmate! \n\n                          White wins!";
                                        break;
                                    case 2:
                                        tempString = "                      Black resigned! \n\n                         White wins!";
                                        break;
                                    case 3:
                                        tempString = "                  Black ran out of time! \n\n                         White wins!";
                                        break;
                                }
                            }
                            else if (drawType > 0)
                            {
                                switch (drawType)
                                {
                                    case 1:
                                        tempString = "\n                   Draw by stalemate!\n";
                                        break;
                                    case 2:
                                        tempString = "\n                 Draw by 50-move rule!\n";
                                        break;
                                    case 3:
                                        tempString = "\n             Draw by threefold repetition!\n";
                                        break;
                                    case 4:
                                        tempString = "\n             Draw by insufficient material!\n";
                                        break;
                                    case 5:
                                        tempString = "\n                  Draw by agreement!\n";
                                        break;
                                }
                            }
                            break;
                    }
                    break;
                }
            }
        }
        else if (enterNameFlag)
            tempString = "         Please type in the game's name \n\n     (Run the progr. block with said name)";
        else
        {
            if (gameState == 2)
                tempString = "\n  Would you like to save the modified game?\n";
            else
                tempString = "\n Would you like to load or delete this game?\n"; ;
        }
        wString += LCD_DrawSpace(4) + LCD_DrawLine() + LCD_DrawSpace(2) + tempString + LCD_DrawSpace(3) + LCD_DrawLine();
        bString += LCD_DrawSpace(4) + LCD_DrawLine() + LCD_DrawSpace(2) + tempString + LCD_DrawSpace(3) + LCD_DrawLine();
        if(additionalChoiceFlag)
        {
            wString += LCD_DrawSpace(2) + LCD_DrawLine();
            bString += LCD_DrawSpace(2) + LCD_DrawLine();
            if (gameState == 2)
            {
                wString += "  |      Save      |                          |       Exit       |";
                bString += "  |       Exit       |                          |      Save      |";
            }
            else
            {
                wString += "  |      Load      |  |     Cancel     |  |     Delete     |";
                bString += "  |    Delete    |   |     Cancel     |   |      Load      |";
            }
        }
    }
    else if (gameState == 2)
    {
        for (int i = Math.Max((turnNumber - 20) / 2 * 2, 0); i < Math.Max((turnNumber - 20) / 2 * 2, 0) + 30; i += 2)
        {
            if (i + 1 == chessNotation.Count)
                tempString += $"  {(divergenceTurn / 2 == i / 2 && divergenceTurn >= 0 ? "<> " : turnNumber / 2 == i / 2 ? "-> " : "    ")}{(i + 2) / 2}. {chessNotation[i]}\n";
            else if (i < chessNotation.Count)
                tempString += $"  {(divergenceTurn / 2 == i / 2 && divergenceTurn >= 0 ? "<> " : turnNumber / 2 == i / 2 ? "-> " : "    ")}{(i + 2) / 2}. {chessNotation[i]},   {chessNotation[i + 1]}\n";
            else
                tempString += "\n";
        }
        wString += tempString + LCD_DrawLine() + "  | PrevMove |     | NextMove |     |      Exit      |";
        bString += tempString + LCD_DrawLine() + "  |      Exit      |     | NextMove |     | PrevMove |";
    }
    else if (gameState == 3)
    {
        for (int i = Math.Max(menuChoice - 10, 0); i < Math.Max(menuChoice - 10, 0) + 15; i++)
        {
            if (i == 3)
                tempString += LCD_DrawLine();
            else
            {
                tempString += $"  {((menuChoice == i && i < 3) || (menuChoice == i - 1 && i > 3) ? "-> " : "    ")}";
                if (i == 0)
                    tempString += "Exit\n";
                else if (i == 1)
                    tempString += "Save current game\n";
                else if (i == 2)
                    tempString += "Delete all games\n";
                else if (i < chessNotationsList.Count + 4)
                    tempString += chessGamesNameList[i - 4] + "\n";
                else
                    tempString += "\n";
            }
        }
        wString += tempString + LCD_DrawLine() + "  |    Select    |    |   MoveUp   |   | MoveDown |";
        bString += tempString + LCD_DrawLine() + " | MoveDown |   |   MoveUp   |    |    Select    |";
    }
    else
    {
        string wTimeString = $"{(gameState == 1 ? time[0] : savedTime[0]) / 60:D2}:{(gameState == 1 ? time[0] : savedTime[0]) % 60:D2}";
        string bTimeString = $"{(gameState == 1 ? time[1] : savedTime[1]) / 60:D2}:{(gameState == 1 ? time[1] : savedTime[1]) % 60:D2}";
        wString += "Time: ";
        bString += "Time: ";
        if (isClock[0])
            wString += $"{(savedTime[0] / 60).ToString()}:00 + {(isIncrement[0] && savedIncrement[0] > 0 ? savedIncrement[0].ToString() + "s" : "None")}";
        else
            wString += $"Unlimited time";
        if (isClock[1])
            bString += $"{(savedTime[1] / 60).ToString()}:00 + {(isIncrement[1] && savedIncrement[1] > 0 ? savedIncrement[1].ToString() + "s" : "None")}";
        else
            bString += $"Unlimited time";

        wString += $" | Format: ({(savedTime[0] / 60).ToString()}:00 + {savedIncrement[0].ToString()}s)\n" + LCD_DrawLine();
        bString += $" | Format: ({(savedTime[1] / 60).ToString()}:00 + {savedIncrement[1].ToString()}s)\n" + LCD_DrawLine();
        if (isClock[0] || isClock[1])
        {
            for (int i = isClock[0] ? 0 : 1; i < (isClock[1] ? 2 : 1); i++)
            {
                for (int j = 0; j < 7; j++)
                {
                    foreach (char c in i == 0 ? wTimeString : bTimeString)
                    {
                        if (c == ':')
                        {
                            if (i == 0)
                                wString += j == 3 || j == 6 ? "* " : "  ";
                            else
                                bString += j == 3 || j == 6 ? "* " : "  ";
                        }
                        else if (i == 0)
                            wString += asciiNumbers[((int)c - 48) * 7 + j] + " ";
                        else
                            bString += asciiNumbers[((int)c - 48) * 7 + j] + " ";

                    }
                    if (i == 0)
                        wString += "\n";
                    else
                        bString += "\n";
                }
            }
        }
        if (!(isClock[0] && isClock[1]))
        {
            for (int i = 0; i < 7; i++)
            {
                if (!isClock[0])
                    wString += asciiNumbers[70 + i] + " " + asciiNumbers[77 + i] + "\n";
                if (!isClock[1])
                    bString += asciiNumbers[70 + i] + " " + asciiNumbers[77 + i] + "\n";
            }
        }
        wString += LCD_DrawLine();
        bString += LCD_DrawLine();
        if (gameState == 0)
        {
            wString += LCD_DrawSpace(5) + LCD_DrawLine();
            bString += LCD_DrawSpace(5) + LCD_DrawLine();
            if(chessNotation.Count > 0 || chessNotationsList.Count > 0)
            {
                wString += "  | NewGame |    | Save/Load |   |  Analysis  |";
                bString += "  |  Analysis  |   | Save/Load |    | NewGame |";
            }
        }
        else
        {
            bool isActiveFlag = false;
            for (int i = 0; i < 12; i++)
            {
                if (messageFlags[i] >= 0)
                {
                    wString += LCD_DrawSpace(2);
                    bString += LCD_DrawSpace(2);
                    switch (i)
                    {
                        case 0:
                            wString += "                  TakeBack request sent.";
                            bString += "                White requests TakeBack!";
                            break;
                        case 1:
                            wString += "                Black requests TakeBack!";
                            bString += "                  TakeBack request sent.";
                            break;
                        case 2:
                            wString += "                       Draw offer sent.";
                            bString += "                     White offers draw!";
                            break;
                        case 3:
                            wString += "                     Black offers draw!";
                            bString += "                       Draw offer sent.";
                            break;
                        case 4:
                            wString += $"            Request to turn clock {(isClock[0] ? "off" : "on")} sent.";
                            bString += $"          White requests to turn clock {(isClock[0] ? "off" : "on")}!";
                            break;
                        case 5:
                            wString += $"          Black requests to turn clock {(isClock[1] ? "off" : "on")}!";
                            bString += $"            Request to turn clock {(isClock[1] ? "off" : "on")} sent.";
                            break;
                        case 6:
                            wString += $"         Request to turn increment {(isIncrement[0] ? "off" : "on")} sent.";
                            bString += $"       White requests to turn increment {(isIncrement[0] ? "off" : "on")}!";
                            break;
                        case 7:
                            wString += $"       Black requests to turn increment {(isIncrement[1] ? "off" : "on")}!";
                            bString += $"         Request to turn increment {(isIncrement[1] ? "off" : "on")} sent.";
                            break;
                        case 8:
                            wString += $"         Request to {(timeIncrease[0] > 0 ? "add" : "remove")} {Math.Abs(timeIncrease[0] / 60)} minute{(Math.Abs(timeIncrease[0] / 60) > 1 ? "s" : "")} sent."; //add minute / minutes 
                            bString += $"         White requests to {(timeIncrease[0] > 0 ? "add" : "remove")} {Math.Abs(timeIncrease[0] / 60)} minute{(Math.Abs(timeIncrease[0] / 60) > 1 ? "s" : "")}!";
                            break;
                        case 9:
                            wString += $"         Black requests to {(timeIncrease[1] > 0 ? "add" : "remove")} {Math.Abs(timeIncrease[1] / 60)} minute{(Math.Abs(timeIncrease[1] / 60) > 1 ? "s" : "")}!"; ;
                            bString += $"         Request to {(timeIncrease[1] > 0 ? "add" : "remove")} {Math.Abs(timeIncrease[1] / 60)} minute{(Math.Abs(timeIncrease[1] / 60) > 1 ? "s" : "")} sent.";
                            break;
                        case 10:
                            wString += $"         Request to {(incrementIncrease[0] > 0 ? "add" : "remove")} {Math.Abs(incrementIncrease[0])} increment sent.";
                            bString += $"         White requests to {(incrementIncrease[0] > 0 ? "add" : "remove")} {Math.Abs(incrementIncrease[0])} increment!";
                            break;
                        case 11:
                            wString += $"         Black requests to {(incrementIncrease[1] > 0 ? "add" : "remove")} {Math.Abs(incrementIncrease[1])} increment!";
                            bString += $"         Request to {(incrementIncrease[1] > 0 ? "add" : "remove")} {Math.Abs(incrementIncrease[1])} increment sent.";
                            break;
                    }
                    wString += LCD_DrawSpace(3) + LCD_DrawLine();
                    bString += LCD_DrawSpace(3) + LCD_DrawLine();
                    if (i % 2 == 0)
                    {
                        wString += "                                                    |  Resign  |";
                        bString += "      | Deny |                                    | Accept |";
                    }
                    else
                    {
                        wString += "    | Accept |                                    | Deny |";
                        bString += "   |  Resign  |";
                    }
                    isActiveFlag = true;
                }
            }
            if (!isActiveFlag)
            {
                wString += LCD_DrawSpace(5) + LCD_DrawLine() + $"  {(!whiteTurn && !promotionFlag ? "| TakeBack |" : "                    ")}    | OfferDraw |     |   Resign   |";
                bString += LCD_DrawSpace(5) + LCD_DrawLine() + $"  |   Resign   |     | OfferDraw |     {(whiteTurn && !promotionFlag ? "| TakeBack |" : "")}";
            }
        }
    }
    wPanels[0].WriteText(wString);
    bPanels[0].WriteText(bString);
}

public string LCD_DrawLine()
{
    string line = "";
    for (int i = 0; i < 60; i++)
        line += "-";
    line += "\n";
    return line;
}

public string LCD_DrawSpace(int number)
{
    string space = "";
    for (int i = 0; i < number; i++)
        space += "\n";
    return space;
}

public void LCD_PanelTimesHandler()
{
    for (int i = 0; i < 4; i++)
    {
        if (LCDPanelsTimes[i] > 0)
        {
            if (LCDPanelsTimes[i] - 1 == 0)
            {
                switch(i)
                {
                    case 0:
                        LCD_SetPanelToDefault(true, 3);
                        break;
                    case 1:
                        LCD_SetPanelToDefault(false, 3);
                        break;
                    case 2:
                        for (int j = 1; j < 3; j++)
                            for (int k = 0; k < 2; k++)
                                LCD_SetPanelToDefault(k == 0 ? true : false, j);
                        break;
                    case 3:
                        timeIncrease[0] = timeIncrease[1] = incrementIncrease[0] = incrementIncrease[1] = 0;
                        if (messageFlags[15] >= 0 && gameState == 1)
                            gameState = 0;
                        Runtime.UpdateFrequency &= ~UpdateFrequency.Update100;
                        LCD_SetPanelToDefault(false, 0);
                        LCD_SetPanelToDefault(true, 0);
                        break;
                }
            }
            LCDPanelsTimes[i]--;
        }
    }
}

public void LCD_SetPanelToDefault(bool color, int panelNumber)
{
    int tempInt = 0;
    float tempFloat = 0;
    string tempString = "";
    switch (panelNumber)
    {
        case 0:
            tempInt = 1;
            tempFloat = 0;
            for (int k = 0; k < 16; k++)
                messageFlags[k] = -1;
            if(isMusic)
                Sound_PlayMusic(false);
            LCD_UpdateInfoPanels();
            break;
        case 1:
            tempInt = 5;
            tempFloat = 5;
            tempString = "Clock increment \n        panel";
            break;
        case 2:
            tempInt = 5;
            tempFloat = 0;
            tempString = "\n Clock time panel";
            break;
        case 3:
            tempInt = 5;
            tempFloat = 12.5F;
            tempString = "Piece rotation \n      panel";
            break;
        case 4:
            tempInt = 5;
            tempFloat = 0;
            tempString = "\n Promotion panel";
            break;
    }
    if(color)
    {
        wPanels[panelNumber].FontSize = tempInt;
        wPanels[panelNumber].TextPadding = tempFloat;
        if(panelNumber > 0)
            wPanels[panelNumber].WriteText(tempString);
    } 
    else
    {
        bPanels[panelNumber].FontSize = tempInt;
        bPanels[panelNumber].TextPadding = tempFloat;
        if (panelNumber > 0)
            bPanels[panelNumber].WriteText(tempString);
    }
}

public void Clock_ChessClocksHandler()
{
    if ((isClock[0] || isClock[1] || gameState == 1) && messageFlags[15] == -1)
    {
        ElapsedTime += Runtime.TimeSinceLastRun;
        if ((int)ElapsedTime.TotalSeconds >= 1)
        {
            int elapsedSeconds = (int)ElapsedTime.TotalSeconds;
            ElapsedTime -= TimeSpan.FromSeconds(elapsedSeconds);

            if (whiteTurn && isClock[0] && time[0] > 0)
                    time[0] -= elapsedSeconds;
            else if (!whiteTurn && isClock[1] && time[1] > 0)
                    time[1] -= elapsedSeconds;
            if (time[0] == 0 || time[1] == 0)
            {
                if (time[0] == 0)
                    wLossType = 3;
                else
                    bLossType = 3;
                messageFlags[15] = 1;
                LCDPanelsTimes[3] = endOfGameMessageTime;
                Runtime.UpdateFrequency |= UpdateFrequency.Update100;
            }
            LCD_UpdateInfoPanels();
        }
    }
    else
        Runtime.UpdateFrequency &= ~UpdateFrequency.Update10;
}

public void Sound_PlayGameSounds(bool isValidMove)
{
    if(!isValidMove)
        foreach (IMySoundBlock block in soundBlocks)
        {
            block.SelectedSound = moveNotAllowedSound.SoundName;
            block.Volume = moveNotAllowedSound.Volume;
            block.Play();
        }
    else if(animationFlag)
        foreach (IMySoundBlock block in soundBlocks)
        {
            block.SelectedSound = moveSound.SoundName;
            block.Volume = moveSound.Volume;
            block.Play();
        }    
    else
    {
        foreach (IMySoundBlock block in soundBlocks)
        {
            block.SelectedSound = arrivalSound.SoundName;
            block.Volume = arrivalSound.Volume;
            block.Play();
        }
        if (moveInfo.isCheck)
            foreach (IMySoundBlock block in soundBlocks)
            {
                block.SelectedSound = checkSound.SoundName;
                block.Volume = checkSound.Volume;
                block.Play();
            }
        else if (moveInfo.isPromotion)
            foreach (IMySoundBlock block in soundBlocks)
            {
                block.SelectedSound = promotionSound.SoundName;
                block.Volume = promotionSound.Volume;
                block.Play();
            }
        else if (moveInfo.isCapture)
            foreach (IMySoundBlock block in soundBlocks)
            {
                block.SelectedSound = captureSound.SoundName;
                block.Volume = captureSound.Volume;
                block.Play();
            }
    }
    foreach (IMySoundBlock block in soundBlocks)
    {
        block.Stop();
    }
}

public void Sound_PlayMusic(bool activation)
{
    if(activation)
        foreach (IMySoundBlock block in soundBlocks)
        {
            block.SelectedSound = gameEndMusic.SoundName;
            block.Volume = gameEndMusic.Volume;
            block.Play();
        }
    else
        foreach (IMySoundBlock block in soundBlocks)
        {
            block.Stop();
        }
}