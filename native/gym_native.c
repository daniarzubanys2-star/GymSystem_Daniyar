#include "gym_native.h"

int gym_bonus_points(int months, int trainer_sessions)
{
    if (months < 1 || months > 36 || trainer_sessions < 0 || trainer_sessions > 1000)
        return -1;

    int points = months * 10 + trainer_sessions * 5;
    if (months >= 12)
        points += 50;          /* long-term loyalty bonus */
    return points;
}
