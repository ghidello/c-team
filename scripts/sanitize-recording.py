"""Create a deliberately lossy, allowlisted protocol derivative for public replay.

This is a spike utility, not a general-purpose redaction guarantee. Review the
result before sharing. Unknown methods and all assistant/tool prose are omitted.
"""
import argparse
import hashlib
import json
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("source", type=Path)
parser.add_argument("destination", type=Path)
args = parser.parse_args()
ids, paths, requests = {}, {}, {}


def ident(value):
    if value is None:
        return None
    return ids.setdefault(value, f"id-{len(ids) + 1:03d}")


def select(obj, keys):
    return {key: obj[key] for key in keys.split() if key in obj}


def thread(obj):
    result = select(obj, "model modelProvider reasoningEffort createdAt updatedAt status agentRole agentNickname historyMode ephemeral")
    for key in ("id", "sessionId", "parentThreadId", "forkedFromId"):
        if key in obj:
            result[key] = ident(obj[key])
    result["turns"] = []
    return result


def turn(obj):
    result = select(obj, "status startedAt completedAt durationMs")
    result.update(id=ident(obj.get("id")), items=[])
    if obj.get("error"):
        result["error"] = {"message": "<recorded error>"}
    return result


def item(obj):
    kind = obj.get("type")
    if kind not in ("subAgentActivity", "commandExecution", "fileChange", "mcpToolCall", "dynamicToolCall", "collabAgentToolCall", "enteredReviewMode", "exitedReviewMode"):
        return None
    result = select(obj, "type kind status durationMs exitCode success")
    result["id"] = ident(obj.get("id"))
    for key in ("agentThreadId", "senderThreadId"):
        if key in obj:
            result[key] = ident(obj[key])
    if "agentPath" in obj:
        result["agentPath"] = "/root/" + ident(obj.get("agentThreadId"))
    if "receiverThreadIds" in obj:
        result["receiverThreadIds"] = [ident(x) for x in obj["receiverThreadIds"]]
    if kind == "commandExecution":
        result["command"] = "<fixture command>"
    if kind in ("mcpToolCall", "dynamicToolCall", "collabAgentToolCall"):
        result["tool"] = "<recorded tool>"
    if kind == "fileChange":
        result["changes"] = [{"path": paths.setdefault(c.get("path"), f"file-{len(paths) + 1}"), "kind": {"type": c.get("kind", {}).get("type", "update")}, "diff": ""} for c in obj.get("changes", [])]
    return result


def sanitize(message, direction):
    method = message.get("method")
    p = message.get("params") or {}
    if direction == "out" and method:
        if method not in ("initialize", "initialized", "thread/start", "turn/start", "thread/read", "review/start"):
            return None
        requests[message.get("id")] = method
        clean = select(p, "model effort historyMode")
        if "threadId" in p:
            clean["threadId"] = ident(p["threadId"])
        if method == "initialize":
            clean = {"clientInfo": {"name": "cteam", "title": "C-Team", "version": "spike"}, "capabilities": p.get("capabilities")}
        if method == "turn/start":
            clean["input"] = [{"type": "text", "text": "<controlled fixture prompt>", "text_elements": []}]
        if method == "review/start":
            clean.update(delivery=p.get("delivery"), target={"type": "uncommittedChanges"})
        result = {"method": method, "params": clean}
        if "id" in message:
            result["id"] = message["id"]
        return result
    if not method:
        request = requests.get(message.get("id"))
        r = message.get("result") or {}
        if not request:
            return None
        if "error" in message:
            return {"id": message["id"], "error": {"code": message["error"].get("code"), "message": "<recorded RPC error>"}}
        clean = select(r, "model reasoningEffort serviceTier")
        if "thread" in r:
            clean["thread"] = thread(r["thread"])
        if "turn" in r:
            clean["turn"] = turn(r["turn"])
        if "reviewThreadId" in r:
            clean["reviewThreadId"] = ident(r["reviewThreadId"])
        return {"id": message["id"], "result": clean}
    clean = {}
    for key in ("threadId", "turnId"):
        if key in p:
            clean[key] = ident(p[key])
    if method == "thread/started":
        clean["thread"] = thread(p["thread"])
    elif method == "thread/status/changed":
        clean["status"] = p.get("status")
    elif method in ("turn/started", "turn/completed"):
        clean["turn"] = turn(p["turn"])
    elif method == "thread/tokenUsage/updated":
        clean["tokenUsage"] = p["tokenUsage"]
    elif method in ("item/started", "item/completed"):
        clean["item"] = item(p["item"])
        if clean["item"] is None:
            return None
    elif method == "turn/plan/updated":
        clean["plan"] = [{"step": f"Step {i + 1}", "status": s["status"]} for i, s in enumerate(p.get("plan", []))]
        clean["explanation"] = None
    elif method == "turn/diff/updated":
        lines = []
        for line in p.get("diff", "").splitlines():
            if line.startswith("diff --git "):
                name = paths.setdefault(line, f"file-{len(paths) + 1}")
                lines.append(f"diff --git a/{name} b/{name}")
            elif line.startswith("@@"):
                lines.append("@@ -1 +1 @@")
            elif line.startswith("+") and not line.startswith("+++"):
                lines.append("+<added>")
            elif line.startswith("-") and not line.startswith("---"):
                lines.append("-<removed>")
        clean["diff"] = "\n".join(lines)
    elif method == "model/rerouted":
        clean.update(select(p, "fromModel toModel reason"))
    else:
        return None
    return {"method": method, "params": clean}


args.destination.parent.mkdir(parents=True, exist_ok=True)
count = 0
with args.source.open(encoding="utf-8-sig") as source, args.destination.open("w", encoding="utf-8") as target:
    for line in source:
        record = json.loads(line)
        direction = record.get("direction")
        if direction not in ("in", "out"):
            continue
        message = record.get("raw", record.get("message"))
        try:
            if isinstance(message, str):
                message = json.loads(message)
        except json.JSONDecodeError:
            continue
        clean = sanitize(message, direction)
        if clean is not None:
            count += 1
            target.write(json.dumps({"timestamp": record["timestamp"], "sequence": count, "direction": direction, "raw": clean}) + "\n")
manifest = {"sourceSha256": hashlib.sha256(args.source.read_bytes()).hexdigest(), "derivativeSha256": hashlib.sha256(args.destination.read_bytes()).hexdigest(), "records": count, "transform": "Allowlisted lossy derivative; IDs pseudonymized; paths, prompts, prose, tool output, code and unknown messages removed. Timestamps, counters, models and lifecycle retained. Review before sharing."}
args.destination.with_suffix(".provenance.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
print(json.dumps(manifest))
