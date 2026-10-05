/**
 * Date handling for the two kinds of date the API returns:
 *
 * - Timestamps (createdAt, dateAdded, expiresAt, ...) are instants in UTC. The API serialises them
 *   without a trailing "Z", so they must be told apart from local times before parsing.
 * - Calendar dates (dateWatched, lastWatchedAt) are the day the user watched something. They are
 *   sent and read as "YYYY-MM-DD" strings and never converted through a Date/time zone, so the
 *   displayed day can't shift.
 */

const MONTHS_SHORT = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
const MONTHS_LONG = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
];

const CALENDAR_DATE = /^(\d{4})-(\d{2})-(\d{2})/;
const HAS_TIME_ZONE = /(Z|[+-]\d{2}:?\d{2})$/i;

/** Parses a server timestamp, treating one without a time zone designator as UTC. */
export function parseServerTimestamp(value: string): Date {
  return new Date(HAS_TIME_ZONE.test(value) ? value : `${value}Z`);
}

/** Extracts the calendar date ("YYYY-MM-DD") from a server date value such as "2026-09-28T00:00:00". */
export function toCalendarDate(value: string): string {
  const match = CALENDAR_DATE.exec(value);
  if (!match) {
    throw new Error(`Not a calendar date: ${value}`);
  }
  return `${match[1]}-${match[2]}-${match[3]}`;
}

/** Formats a calendar date as "28 Sep 2026" without any time zone conversion. */
export function formatCalendarDate(value: string): string {
  const [year, month, day] = toCalendarDate(value).split('-');
  return `${day} ${MONTHS_SHORT[Number(month) - 1]} ${year}`;
}

/** Formats a server timestamp as a date ("21 Sep 2026") in the user's local time zone. */
export function formatTimestampDate(value: string): string {
  const date = parseServerTimestamp(value);
  return `${pad(date.getDate())} ${MONTHS_SHORT[date.getMonth()]} ${date.getFullYear()}`;
}

/** "March 2025" for a server timestamp, in local time. */
export function formatTimestampMonthYear(value: string): string {
  const date = parseServerTimestamp(value);
  return `${MONTHS_LONG[date.getMonth()]} ${date.getFullYear()}`;
}

/** "September 2026" for the given (default: current) local date. */
export function formatMonthYear(date: Date = new Date()): string {
  return `${MONTHS_LONG[date.getMonth()]} ${date.getFullYear()}`;
}

/** Today's date in the user's local time zone as "YYYY-MM-DD". */
export function todayCalendarDate(now: Date = new Date()): string {
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
}

export function isValidCalendarDate(value: string): boolean {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  if (!match) {
    return false;
  }
  const [year, month, day] = [Number(match[1]), Number(match[2]), Number(match[3])];
  const date = new Date(Date.UTC(year, month - 1, day));
  return date.getUTCFullYear() === year && date.getUTCMonth() === month - 1 && date.getUTCDate() === day;
}

/** True when a "YYYY-MM-DD" date is after today (local). ISO dates compare correctly as strings. */
export function isFutureCalendarDate(value: string, now: Date = new Date()): boolean {
  return value > todayCalendarDate(now);
}

/** True when a server timestamp falls within the last `days` days. */
export function isWithinLastDays(value: string, days: number, now: Date = new Date()): boolean {
  const time = parseServerTimestamp(value).getTime();
  return time <= now.getTime() && now.getTime() - time <= days * 24 * 60 * 60 * 1000;
}

function pad(value: number): string {
  return String(value).padStart(2, '0');
}
