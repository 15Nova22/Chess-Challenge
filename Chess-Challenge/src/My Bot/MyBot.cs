
﻿using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    readonly int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    int counterRochade = 0;
    readonly string[,] opening = new string[,] {  {"g1f3","b1c3","e2e4","d2d4","f1d3","c1e3","e1g1"} , {"g8f6", "b8c6","d7d5","e7e5","f8d6","c8e6","e8g8"}  };
    public Move Think(Board board, Timer timer)
    {
        //Filter all Moves, that do not end in draw
        Move[] allMoves = board.GetLegalMoves()
            .Where(move => {
                board.MakeMove(move); 
                var isNotDraw = !board.IsDraw(); 
                board.UndoMove(move); 
                return isNotDraw; 
            })
            .ToArray();

        if (allMoves.Length == 0) return board.GetLegalMoves().First();

        //All possible moves, that can capture.
        Move[] captureMoves = board.GetLegalMoves(true);

        //Order capture moves by value difference
        Move bestCapture = captureMoves
            //Nur trade up
            .Where(move => TradeValue(board, move) > 0)
            //Order by best trade
            .OrderByDescending(move => TradeValue(board, move)
        //Use best trade
        ).FirstOrDefault();


        //Possible enemy counters
        board.MakeMove(bestCapture);
        var enemyTargets = board.GetLegalMoves(true).Select(move => move.TargetSquare).ToArray();
        Move[] gegnerStartAngriff = board.GetLegalMoves(true);
        board.UndoMove(bestCapture);

        //Save queen if under attack
        if (board.TrySkipTurn())
        {
            var gegnerDamenZiele = board.GetLegalMoves(true).Where(move => pieceValues[(int)move.CapturePieceType] == 900).ToArray();
            board.UndoSkipTurn();
            if (gegnerDamenZiele.Length > 0)
            {
                var dameIstSicher = allMoves.Where(move => !board.SquareIsAttackedByOpponent(move.TargetSquare)).Where(move => pieceValues[(int)move.MovePieceType] == 900).ToArray();
                foreach (Move move in dameIstSicher)
                {
                    board.MakeMove(move);
                    if (captureMoves.Contains(move) && !board.SquareIsAttackedByOpponent(move.TargetSquare))
                    {
                        board.UndoMove(move);
                        return move;
                    }
                    board.UndoMove(move);
                }
                if(dameIstSicher.Length > 0) return dameIstSicher.FirstOrDefault();
            }
        }

        //Moves that have an unprotected start square
        //Compares if the moves are protected in a positive trade
        var unprotected = allMoves
            .Where(move => enemyTargets.Contains(move.StartSquare))
            .Where(move => !IsProtected(move, board)).OrderByDescending(move => pieceValues[(int)move.MovePieceType]).ToArray();
       

        if(!bestCapture.IsNull && IsWorthToTrade(bestCapture, unprotected.FirstOrDefault()) && !board.SquareIsAttackedByOpponent(bestCapture.TargetSquare))
        {
            return bestCapture;
        }

        //Check if a move can additionally protect an unprotected square
        foreach (Move movey in allMoves)
        {
            string neuerStartSquare = movey.TargetSquare.Name;
            board.MakeMove(movey);
            bool temp = board.TrySkipTurn();
            //Enemy Moves
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

        if (!protectedTargetSquare.IsNull) return protectedTargetSquare;


        //Rochade, fixed, per Color
        if (counterRochade < opening.GetLength(1)) {
            //Undo Move if it captured
            int farbe = board.IsWhiteToMove ? 0 : 1;
            string code = opening[farbe, counterRochade];            
            Move moveRochade = allMoves.Where(move => move.StartSquare.Name == code.Substring(0, 2) && move.TargetSquare.Name == code.Substring(2, 2)).FirstOrDefault();
            counterRochade++;
            if(!moveRochade.IsNull) return moveRochade;
        }

        //Prefer moves that reduce enemy moves
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


        //Random Move if nothing else can be done
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