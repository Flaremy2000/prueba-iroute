import { formatIsoDate, formatNumber, isIsoDate, todayIso } from './date';

describe('date utils', () => {
  it('generates an ISO date for today', () => {
    expect(todayIso()).toMatch(/^\d{4}-\d{2}-\d{2}$/);
  });

  it('validates ISO date strings', () => {
    expect(isIsoDate('2026-11-01')).toBe(true);
    expect(isIsoDate('2026-02-29')).toBe(false);
    expect(isIsoDate('invalid')).toBe(false);
    expect(isIsoDate('')).toBe(false);
    expect(isIsoDate(null)).toBe(false);
  });

  it('formats ISO dates for display', () => {
    const formatted = formatIsoDate('2026-11-01');
    expect(formatted).toContain('2026');
    expect(formatIsoDate(null)).toBe('—');
    expect(formatIsoDate('not-a-date')).toBe('not-a-date');
  });

  it('formats numbers with Colombian locale separator', () => {
    expect(formatNumber(1250000)).toBe('1.250.000');
  });
});
