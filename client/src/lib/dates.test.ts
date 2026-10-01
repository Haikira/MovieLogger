import { describe, expect, it } from 'vitest';
import {
  formatCalendarDate,
  isFutureCalendarDate,
  isValidCalendarDate,
  isWithinLastDays,
  parseServerTimestamp,
  toCalendarDate,
  todayCalendarDate,
} from './dates';

describe('parseServerTimestamp', () => {
  it('treats a timestamp without a time zone as UTC', () => {
    expect(parseServerTimestamp('2026-09-30T23:30:00').toISOString()).toBe('2026-09-30T23:30:00.000Z');
  });

  it('respects an explicit Z or offset', () => {
    expect(parseServerTimestamp('2026-09-30T23:30:00Z').toISOString()).toBe('2026-09-30T23:30:00.000Z');
    expect(parseServerTimestamp('2026-09-30T23:30:00+01:00').toISOString()).toBe('2026-09-30T22:30:00.000Z');
  });
});

describe('calendar dates', () => {
  it('formats the watched date exactly as stored, with no time zone shift', () => {
    // Midnight values are where naive Date parsing shifts the day in time zones behind/ahead of UTC.
    expect(formatCalendarDate('2026-09-01T00:00:00')).toBe('01 Sep 2026');
    expect(formatCalendarDate('2025-12-31')).toBe('31 Dec 2025');
  });

  it('extracts the date part of a server value', () => {
    expect(toCalendarDate('2026-09-28T00:00:00')).toBe('2026-09-28');
    expect(() => toCalendarDate('yesterday')).toThrow();
  });

  it("uses the local date for 'today'", () => {
    expect(todayCalendarDate(new Date(2026, 8, 30, 23, 59))).toBe('2026-09-30');
    expect(todayCalendarDate(new Date(2026, 0, 5, 0, 1))).toBe('2026-01-05');
  });

  it('rejects dates after today', () => {
    const now = new Date(2026, 8, 30, 12, 0);
    expect(isFutureCalendarDate('2026-09-30', now)).toBe(false);
    expect(isFutureCalendarDate('2026-10-01', now)).toBe(true);
    expect(isFutureCalendarDate('2025-12-31', now)).toBe(false);
  });

  it('validates real calendar dates', () => {
    expect(isValidCalendarDate('2024-02-29')).toBe(true);
    expect(isValidCalendarDate('2025-02-29')).toBe(false);
    expect(isValidCalendarDate('2025-13-01')).toBe(false);
    expect(isValidCalendarDate('')).toBe(false);
  });
});

describe('isWithinLastDays', () => {
  it('counts UTC timestamps within the window', () => {
    const now = new Date('2026-09-30T12:00:00Z');
    expect(isWithinLastDays('2026-09-25T12:00:00', 7, now)).toBe(true);
    expect(isWithinLastDays('2026-09-20T12:00:00', 7, now)).toBe(false);
  });
});
