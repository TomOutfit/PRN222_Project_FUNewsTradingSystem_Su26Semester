#!/usr/bin/env python3
"""
TradingAgents Adapter
Input  port: CLI args  argv[1]=TICKER  argv[2]=YYYY-MM-DD (optional, defaults to yesterday)
Output port: single JSON object on stdout (success or {"error":"..."} on failure); exit 1 on error
Env:  OPENAI_API_KEY, TRADINGAGENTS_* (picked up automatically by default_config)
"""
import sys
import os
import json

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))


def main():
    if len(sys.argv) < 2:
        _fail("Usage: ta_adapter.py <TICKER> [YYYY-MM-DD]")

    ticker = sys.argv[1].upper().strip()
    date = sys.argv[2] if len(sys.argv) > 2 else _yesterday()

    try:
        from tradingagents.default_config import DEFAULT_CONFIG
        from tradingagents.graph.trading_graph import TradingAgentsGraph
    except ImportError as e:
        _fail(f"Import error — is scripts/tradingagents/ present and deps installed? {e}")

    config = DEFAULT_CONFIG.copy()
    ta = TradingAgentsGraph(debug=False, config=config)
    state, signal = ta.propagate(ticker, date)

    decision = _to_decision(signal)
    content = _build_content(state)

    print(json.dumps({
        "decision":              decision,
        "title":                 f"[{decision}] {ticker} — TradingAgents Multi-Agent Analysis",
        "headline":              _headline(state, ticker),
        "content":               content,
        "source":                (
            f"TradingAgents | {config['llm_provider'].upper()} "
            f"({config['quick_think_llm']} / {config['deep_think_llm']}) "
            f"| yfinance + FRED + StockTwits"
        ),
        "confidenceScore":       _confidence(state),
        "signal":                signal,
        "ticker":                ticker,
        "marketReport":          state.get("market_report",          ""),
        "sentimentReport":       state.get("sentiment_report",       ""),
        "newsReport":            state.get("news_report",            ""),
        "fundamentalsReport":    state.get("fundamentals_report",    ""),
        "investmentPlan":        state.get("investment_plan",        ""),
        "traderInvestmentPlan":  state.get("trader_investment_plan", ""),
        "finalTradeDecision":    state.get("final_trade_decision",   ""),
    }, ensure_ascii=False), flush=True)


# ── pure helpers ─────────────────────────────────────────────────────────────

def _to_decision(s):
    return {
        "Buy": "BUY", "Overweight": "BUY",
        "Hold": "HOLD", "Neutral": "HOLD",
        "Underweight": "SELL", "Sell": "SELL",
    }.get(s, "HOLD")


def _headline(state, ticker):
    text = state.get("final_trade_decision") or state.get("trader_investment_plan") or ""
    for line in text.splitlines():
        c = line.lstrip("#*- ").strip()
        if len(c) >= 40:
            return c[:250]
    return f"Multi-agent quantitative analysis for {ticker}."


def _build_content(state):
    pairs = [
        ("Sentiment Analysis",        state.get("sentiment_report",       "")),
        ("Fundamental Analysis",      state.get("fundamentals_report",    "")),
        ("News & Macro Report",       state.get("news_report",            "")),
        ("Technical Market Analysis", state.get("market_report",          "")),
        ("Trader Investment Plan",    state.get("trader_investment_plan", "")),
        ("Final Trade Decision",      state.get("final_trade_decision",   "")),
    ]
    return "\n\n---\n\n".join(
        f"## {title}\n{body.strip()}" for title, body in pairs if body.strip()
    )


def _confidence(state):
    scores = [
        _score(state.get("investment_plan",         "")),
        _score(state.get("trader_investment_plan",  "")),
        _score(state.get("final_trade_decision",    "")),
    ]
    non_empty = [s for s in scores if s is not None]
    if not non_empty:
        return 70
    dominant = max(set(non_empty), key=non_empty.count)
    return 60 + int(non_empty.count(dominant) / len(non_empty) * 30)


def _score(text):
    if not text:
        return None
    t = text.lower()
    b = sum(t.count(w) for w in ("buy", "overweight", "long", "bullish"))
    s = sum(t.count(w) for w in ("sell", "underweight", "short", "bearish"))
    return "B" if b > s else ("S" if s > b else "H")


def _yesterday():
    from datetime import datetime, timedelta, timezone
    return (datetime.now(timezone.utc) - timedelta(days=1)).strftime("%Y-%m-%d")


def _fail(msg):
    print(json.dumps({"error": msg}), flush=True)
    sys.exit(1)


if __name__ == "__main__":
    main()
