
﻿using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    int takenPieces = 0;
    int lostPieces = 0;
    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();

        //Array mit möglichen Zügen zum schlagen, wenn schlagen möglich ist, wird geschlagen
        Move[] moves2 = board.GetLegalMoves(true);
        foreach (Move schlagen in moves2)
        {
            if (!board.SquareIsAttackedByOpponent(schlagen.TargetSquare))
            {
                return schlagen;
            }
            else
            {
                int tempUnserValue = pieceValues[(int) schlagen.MovePieceType];
                int tempSeinValue = pieceValues[(int) schlagen.CapturePieceType];
                   
                if(tempSeinValue > tempUnserValue)
                {
                    takenPieces += tempSeinValue;
                    return schlagen;
                }
            }
        }

        

        Random rng = new();
        int temp = rng.Next(0, moves.Length);
        return moves[temp];
    }
}