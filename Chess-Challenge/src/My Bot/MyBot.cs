
﻿using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    int counterRochade = 0;
    string[,] eroeffnung = new string[,] {  {"g1f3","b1c3","e2e4","d2d4","f1d3","c1e3","e1g1"} , {"g8f6", "b8c6","d7d5","e7e5","f8d6","c8e6","e8g8"}  };
    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();
        Console.WriteLine(allMoves.Length);
        //ToDo direkt alle rausfilter, die beim ziehen zu nem Draw führen würden, damit später nicht auf isDraw überprüft werden muss
        allMoves = allMoves.Where(move => {
            board.MakeMove(move); 
            var isNotDraw = !board.IsDraw(); 
            board.UndoMove(move); 
            return isNotDraw; 
        })
        .ToArray();

        Console.WriteLine(allMoves.Length);

        //Array mit möglichen Zügen zum schlagen, wenn schlagen möglich ist, wird geschlagen
        Move[] captureMoves = board.GetLegalMoves(true);

        //Alle Züge zum Schlagen Sortiert nach ihrem Wert Unterschied
        Move bestCapture = captureMoves
            //Nur positive Trades nehmen
            .Where(move => TradeValue(board, move) > 0)
            //Sortieren nach bestem Trade
            .OrderByDescending(move => TradeValue(board, move)
        //Zug mit bestem Wertverhältnis nehmen
        ).FirstOrDefault();


        //Bildet ein Array aus Gegner Moves, die schlagen können
        board.MakeMove(bestCapture);
        var enemyTargets = board.GetLegalMoves(true).Select(move => move.TargetSquare).ToArray();
        board.UndoMove(bestCapture);
        var gegnerZiele = board.GetLegalMoves(true).Where(move => pieceValues[(int)move.CapturePieceType] == 900).ToArray();
        if (board.TrySkipTurn()) board.UndoSkipTurn();

        //Moves mit ungeschütztem StartSquare
        //Vergleicht unsere Legal Moves mit den SchlagMoves des Gegners und überprüft, ob diese Protected sind 
        var unprotected = allMoves
            .Where(move => enemyTargets.Contains(move.StartSquare))
            .Where(move => !IsProtected(move, board)).OrderByDescending(move => pieceValues[(int)move.MovePieceType]).ToArray();


        //Abschnitt, der die Dame ausser Gefahr bringen soll, wenn diese angegriffen wird
        if(gegnerZiele.Length > 0)
        {
            var dameIstSicher = allMoves.Where(move => !board.SquareIsAttackedByOpponent(move.TargetSquare)).Where(move => pieceValues[(int)move.MovePieceType] == 900).ToArray();
            foreach(Move move in dameIstSicher)
            {
                foreach(Move moveU in unprotected)
                {
                    if(move.TargetSquare.Name == moveU.StartSquare.Name)
                    {
                        return move;
                    }
                }
                return dameIstSicher.FirstOrDefault();
            }
        }


        if(!bestCapture.IsNull && IsWorthToTrade(bestCapture, unprotected.FirstOrDefault()) && !board.SquareIsAttackedByOpponent(bestCapture.TargetSquare))
        {
            return bestCapture;
        }

        //Moves machen, und dann gucken, ob der Move als neuen TargetSquare ein Square hat, das ein unprotected Square schützt
        //altes targetsquare == neues startsquare, für das neue startsquare prüfen ob als targetsquare ein unprotected square ist
        foreach (Move movey in allMoves)
        {
            string neuerStartSquare = movey.TargetSquare.Name;
            board.MakeMove(movey);
            //Zug muss übersprungen werden, um unsere Moves zu kriegen
            bool temp = board.TrySkipTurn();
            //GegnerMoves
            Move[] neueMoves = board.GetLegalMoves();
            if(temp) board.UndoSkipTurn();

            var hasMove = neueMoves
                .Where(move => move.StartSquare.Name == neuerStartSquare)
                .Where(move => unprotected.Select(u => u.StartSquare.Name).Contains(move.TargetSquare.Name))
                .Any();
            board.UndoMove(movey);
            if (hasMove) return movey;
        }

        var protectedTargetSquare = unprotected
            .Where(move => !board.SquareIsAttackedByOpponent(move.TargetSquare)).FirstOrDefault();

        if (!protectedTargetSquare.IsNull && !board.IsDraw()) return protectedTargetSquare;


        //Routine für Rochade, fester Ablauf, je nach Farbe
        if (counterRochade < eroeffnung.GetLength(1)) {
            //Hat eine Figur während der Rochade geschlagen und wird vom Gegner bedroht, wird der Zug zurückgenommen
            int farbe = board.IsWhiteToMove ? 0 : 1;
            string code = eroeffnung[farbe, counterRochade];            
            Move moveRochade = allMoves.Where(move => move.StartSquare.Name == code.Substring(0, 2) && move.TargetSquare.Name == code.Substring(2, 2)).FirstOrDefault();
            counterRochade++;
            if(!moveRochade.IsNull) return moveRochade;
        }

        //Zug der die Anzahl der Legal Moves des Gegners immer weiter verringert
        Move movetemp = allMoves.FirstOrDefault();
        int laengeArrayGegnerMoves = 100000;
        Board boardTemp = board;
        foreach(Move movex in allMoves)
        {
            board.MakeMove(movex);
            boardTemp = board;
            Move[] gegnerMoves = board.GetLegalMoves();
            board.UndoMove(movex);
            if (gegnerMoves.Length < laengeArrayGegnerMoves && !board.SquareIsAttackedByOpponent(movex.TargetSquare) && !boardTemp.IsDraw())
            {   
                laengeArrayGegnerMoves = gegnerMoves.Length;
                movetemp = movex;

                if (pieceValues[(int)movex.CapturePieceType] == 10000)
                {
                    return movex;
                }
            }
        }

        if(movetemp != allMoves.FirstOrDefault() && !boardTemp.IsDraw())
        {
            return movetemp;
        }


        //Zufälliger Zug wenn sonst nichts machbar ist
        Random rng = new();
        var saveMoves = allMoves.Where(move => !board.SquareIsAttackedByOpponent(move.TargetSquare)).ToArray();
        if (saveMoves.Length > 0 && !board.IsDraw())
        {
            return saveMoves[rng.Next(saveMoves.Length)];
        }

        var fallback = board.GetLegalMoves();
        return fallback[rng.Next(fallback.Length)];
    }

    /// <summary>
    /// Calculates the Trade Value for a move
    /// </summary>
    /// <param name="board">The board</param>
    /// <param name="move">The move to play</param>
    /// <returns>
    ///     The value difference between the capturing piece and the captured piece or just the value of the captured piece if no recapture is possible
    /// </returns>
    int TradeValue(Board board, Move move)
    {
        return pieceValues[(int)move.CapturePieceType] - (board.SquareIsAttackedByOpponent(move.TargetSquare) ? pieceValues[(int)move.MovePieceType] : 0);
    }

    bool IsProtected(Move moveWithTarget, Board board)
    {
        if(board.TrySkipTurn())
        {
            var enemyMove = board.GetLegalMoves(true).Where(move => move.TargetSquare == moveWithTarget.StartSquare).FirstOrDefault();
            if(!enemyMove.IsNull)
            {
                board.MakeMove(enemyMove);
                var hasCounterMoves = board.GetLegalMoves(true).Where(move => move.TargetSquare == moveWithTarget.StartSquare).Any();
                board.UndoMove(enemyMove);
                board.UndoSkipTurn();
                return hasCounterMoves;
            }
            board.UndoSkipTurn();
        }
        return false;
    }

    bool IsWorthToTrade(Move bestCapture, Move isAttacked)
    {
        int valueBestCapture = pieceValues[(int)bestCapture.CapturePieceType];
        int valueBeschuetzen = pieceValues[(int)isAttacked.MovePieceType];
        return valueBestCapture >= valueBeschuetzen;
    }
}