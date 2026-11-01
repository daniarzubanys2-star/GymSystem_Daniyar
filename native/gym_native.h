#ifndef GYM_NATIVE_H
#define GYM_NATIVE_H

/* Export macro: Windows needs __declspec(dllexport), Linux/macOS use default visibility. */
#ifdef _WIN32
#define GYM_API __declspec(dllexport)
#else
#define GYM_API __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif

/*
 * Loyalty bonus points of a gym client.
 *   months           : 1..36      (membership length)
 *   trainer_sessions : 0..1000    (personal-trainer sessions)
 * Returns months*10 + trainer_sessions*5, plus 50 if months >= 12.
 * Returns -1 if an argument is outside the valid range.
 * Only simple int values are used: no memory is allocated, nothing has to be released.
 */
GYM_API int gym_bonus_points(int months, int trainer_sessions);

#ifdef __cplusplus
}
#endif

#endif
