namespace GymSystem;

// All business numbers are in one place, so they are easy to find and to change.
// Money is stored as whole units (int) to avoid rounding surprises.
public static class Rules
{
    public const int MinimumAge = 16;            // clients younger than this are not eligible
    public const int MaxAge = 120;
    public const int MaxMonths = 36;

    public const int TrainerSessionPrice = 20;   // price of one personal-trainer session

    public const int LongTermMonths = 12;        // 12 months or more -> long-term discount
    public const int LongTermDiscountPercent = 10;

    public const int StudentMaxAge = 25;         // used by the Student pricing policy
    public const int StudentDiscountPercent = 20;
}
