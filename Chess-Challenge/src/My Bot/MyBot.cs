
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
        foreach(Move test in allMoves)
        {
            Console.WriteLine(test.StartSquare.Name);
        }

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


        //TODO Save attacked pieces
        //Moves mit ungeschütztem StartSquare
        var unprotected = allMoves
            .Where(move => board.SquareIsAttackedByOpponent(move.StartSquare))
            .Where(move => !isProtected(move.StartSquare, allMoves, board)).OrderByDescending(move => pieceValues[(int)move.MovePieceType]).ToArray();

        if(!bestCapture.IsNull && isWorthToTrade(bestCapture, unprotected.FirstOrDefault()))
        {
            return bestCapture;
        }

        //Moves machen, und dann gucken, ob der Move als neuen TargetSquare ein Square hat, das ein unprotected Square schützt
        //altes targetsquare == neues startsquare, für das neue startsquare prüfen ob als targetsquare ein unprotected square ist
        foreach (Move movey in allMoves)
        {
            String neuerStartSquare = movey.TargetSquare.Name;
            board.MakeMove(movey);
            //Zug muss übersprungen werden, um unsere Moves zu kriegen
            bool erfolgreich = board.TrySkipTurn();
            //GegnerMoves
            Move[] neueMoves = board.GetLegalMoves();
            if(erfolgreich) board.UndoSkipTurn();
            foreach (Move movex in neueMoves)
            {
                if (movex.StartSquare.Name == neuerStartSquare)
                {
                    foreach (Move movez in unprotected)
                    {
                        if (movex.TargetSquare.Name == movez.StartSquare.Name)
                        {
                            board.UndoMove(movey);
                            return movey;
                        }
                    }
                }
            }
            board.UndoMove(movey);
        }

        var protectedTargetSquare = unprotected
            .Where(move => !board.SquareIsAttackedByOpponent(move.TargetSquare)).FirstOrDefault();

        if (!protectedTargetSquare.IsNull)
        {
            return protectedTargetSquare;
        }


        //Routine für Rochade, fester Ablauf, je nach Farbe
        if (counterRochade < eroeffnung.GetLength(1)) {
            //Hat eine Figur während der Rochade geschlagen und wird vom Gegner bedroht, wird der Zug zurückgenommen
            int farbe = board.IsWhiteToMove ? 0 : 1;
            string code = eroeffnung[farbe, counterRochade];            
            Move moveRochade = allMoves.Where(move => move.StartSquare.Name == code.Substring(0, 2) && move.TargetSquare.Name == code.Substring(2, 2)).FirstOrDefault();
            counterRochade++;
            if(!moveRochade.IsNull) return moveRochade;
        }



        //Zufälliger Zug wenn sonst nichts machbar ist
        Random rng = new();
        var saveMoves = allMoves.Where(move => !board.SquareIsAttackedByOpponent(move.TargetSquare)).ToArray();
        if (saveMoves.Length > 0)
        {
            return saveMoves[rng.Next(saveMoves.Length)];
        }

        return allMoves[rng.Next(allMoves.Length)];
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
        Console.WriteLine("MovePieceType" + pieceValues[(int)move.MovePieceType]);
        Console.WriteLine("CapturePieceValue" + pieceValues[(int)move.CapturePieceType]);
        Console.WriteLine("ANGRIFF" + board.SquareIsAttackedByOpponent(move.TargetSquare));
        return pieceValues[(int)move.CapturePieceType] - (board.SquareIsAttackedByOpponent(move.TargetSquare) ? pieceValues[(int)move.MovePieceType] : 0);
    }

    bool isProtected(Square target, Move[] moves, Board board)
    {
        return moves.Where(move => move.TargetSquare == target).Any();
    }

    bool isWorthToTrade(Move bestCapture, Move isAttacked)
    {
        int valueBestCapture = pieceValues[(int)bestCapture.CapturePieceType];
        int valueBeschuetzen = pieceValues[(int)isAttacked.MovePieceType];
        return valueBestCapture >= valueBeschuetzen;
    }
}