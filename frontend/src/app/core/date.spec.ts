import { formatIsoDate, formatNumber, isIsoDate, todayIso } from './date';

describe('date helpers', () => {
  it('returns today as an ISO date', () => {
    expect(todayIso()).toMatch(/^\d{4}-\d{2}-\d{2}$/);
  });

  it('formats a valid ISO date', () => {
    expect(formatIsoDate('2026-11-01')).toContain('2026');
    expect(formatIsoDate('2026-11-01')).toContain('01');
  });

  it('returns a dash for empty dates', () => {
    expect(formatIsoDate(null)).toBe('—');
    expect(formatIsoDate(undefined)).toBe('—');
  });

  it('returns the original value when the format is unexpected', () => {
    expect(formatIsoDate('not-a-date')).toBe('not-a-date');
  });

  it('validates ISO dates', () => {
    expect(isIsoDate('2026-11-01')).toBe(true);
    expect(isIsoDate('2024-02-29')).toBe(true);
    expect(isIsoDate('2025-02-29')).toBe(false);
    expect(isIsoDate('2026-13-01')).toBe(false);
    expect(isIsoDate('01/11/2026')).toBe(false);
    expect(isIsoDate(null)).toBe(false);
  });

  it('formats numbers with the Colombian locale', () => {
    expect(formatNumber(12345)).toBe('12.345');
  });
});
