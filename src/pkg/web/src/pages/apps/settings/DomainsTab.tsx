import { useContext, useState, useEffect } from "react";
import { toast } from "react-toastify";
import { AppContext } from "..";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Loader2,
  Globe,
  Plus,
  Trash2,
  RefreshCw,
  CheckCircle,
  AlertCircle,
  Clock,
  Copy,
} from "lucide-react";
import { useApiClient } from "@/hooks";
import { ICustomDomain } from "./types";

export default function DomainsTab() {
  const { get, post, del } = useApiClient();
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();

  const [loading, setLoading] = useState(true);
  const [domains, setDomains] = useState<ICustomDomain[]>([]);
  const [showAddDialog, setShowAddDialog] = useState(false);
  const [newDomain, setNewDomain] = useState("");
  const [saving, setSaving] = useState(false);
  const [verifyingId, setVerifyingId] = useState<number | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<ICustomDomain | null>(null);
  const [deleting, setDeleting] = useState(false);

  const cnameTarget = domains[0]?.cnameTarget ?? "cname.mycrocloud.site";

  useEffect(() => {
    fetchDomains();
  }, [app.id]);

  const fetchDomains = async () => {
    try {
      const data = await get<ICustomDomain[]>(`/api/apps/${app.id}/customdomains`);
      setDomains(data);
    } finally {
      setLoading(false);
    }
  };

  const handleAdd = async () => {
    const trimmed = newDomain.trim().toLowerCase();
    if (!trimmed) return;
    setSaving(true);
    try {
      await post(`/api/apps/${app.id}/customdomains`, { domain: trimmed });
      toast.success("Domain added. Configure your CNAME record and verify.");
      setShowAddDialog(false);
      setNewDomain("");
      fetchDomains();
    } catch {
      toast.error("Failed to add domain");
    } finally {
      setSaving(false);
    }
  };

  const handleVerify = async (id: number) => {
    setVerifyingId(id);
    try {
      const result = await post<{ status: string; message: string }>(
        `/api/apps/${app.id}/customdomains/${id}/verify`,
        {}
      );
      if (result.status === "Active") {
        toast.success(result.message);
      } else {
        toast.error(result.message);
      }
      fetchDomains();
    } catch {
      toast.error("Verification request failed");
    } finally {
      setVerifyingId(null);
    }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    setDeleting(true);
    try {
      await del(`/api/apps/${app.id}/customdomains/${deleteTarget.id}`);
      toast.success("Domain removed");
      setDeleteTarget(null);
      fetchDomains();
    } catch {
      toast.error("Failed to remove domain");
    } finally {
      setDeleting(false);
    }
  };

  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text);
    toast.success("Copied to clipboard");
  };

  const statusBadge = (status: ICustomDomain["status"]) => {
    switch (status) {
      case "Active":
        return (
          <Badge variant="outline" className="bg-green-500/10 text-green-700 border-green-200">
            <CheckCircle className="mr-1 h-3 w-3" />
            Active
          </Badge>
        );
      case "Pending":
        return (
          <Badge variant="outline" className="bg-yellow-500/10 text-yellow-700 border-yellow-200">
            <Clock className="mr-1 h-3 w-3" />
            Pending
          </Badge>
        );
      case "Failed":
        return (
          <Badge variant="outline" className="bg-red-500/10 text-red-700 border-red-200">
            <AlertCircle className="mr-1 h-3 w-3" />
            Failed
          </Badge>
        );
    }
  };

  if (loading) {
    return (
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Globe className="h-4 w-4 text-muted-foreground" />
            <CardTitle className="text-base">Custom Domains</CardTitle>
          </div>
        </CardHeader>
        <CardContent>
          <div className="flex items-center gap-2 text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading...
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <>
      {/* CNAME Instructions */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">DNS Configuration</CardTitle>
          <CardDescription>
            To use a custom domain, add a CNAME record pointing to the target below.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex items-center gap-2 rounded-lg border bg-muted/50 p-3">
            <div className="flex-1">
              <p className="text-xs text-muted-foreground mb-1">CNAME Target</p>
              <code className="text-sm font-medium">{cnameTarget}</code>
            </div>
            <Button variant="ghost" size="sm" onClick={() => copyToClipboard(cnameTarget)}>
              <Copy className="h-4 w-4" />
            </Button>
          </div>
          <p className="mt-2 text-xs text-muted-foreground">
            DNS changes can take up to 48 hours to propagate. After configuring your CNAME, click "Verify" to activate the domain.
          </p>
        </CardContent>
      </Card>

      {/* Domain List */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <div className="flex items-center gap-2">
                <Globe className="h-4 w-4 text-muted-foreground" />
                <CardTitle className="text-base">Custom Domains</CardTitle>
              </div>
              <CardDescription>
                {domains.length} domain{domains.length !== 1 ? "s" : ""} configured
              </CardDescription>
            </div>
            <Button variant="outline" size="sm" onClick={() => setShowAddDialog(true)}>
              <Plus className="mr-2 h-4 w-4" />
              Add Domain
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          {domains.length === 0 ? (
            <p className="text-sm text-muted-foreground py-4">
              No custom domains configured. Add a domain to get started.
            </p>
          ) : (
            <div className="space-y-2">
              {domains.map((domain) => (
                <div
                  key={domain.id}
                  className="flex items-center justify-between rounded-lg border p-3"
                >
                  <div className="flex items-center gap-3">
                    <code className="text-sm font-medium">{domain.domain}</code>
                    {statusBadge(domain.status)}
                  </div>
                  <div className="flex items-center gap-2">
                    {domain.status !== "Active" && (
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => handleVerify(domain.id)}
                        disabled={verifyingId === domain.id}
                      >
                        {verifyingId === domain.id ? (
                          <Loader2 className="mr-1 h-3 w-3 animate-spin" />
                        ) : (
                          <RefreshCw className="mr-1 h-3 w-3" />
                        )}
                        Verify
                      </Button>
                    )}
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => setDeleteTarget(domain)}
                    >
                      <Trash2 className="h-4 w-4 text-destructive" />
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Add Domain Dialog */}
      <Dialog open={showAddDialog} onOpenChange={setShowAddDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Add Custom Domain</DialogTitle>
            <DialogDescription>
              Enter your domain name. You'll need to configure a CNAME record pointing to{" "}
              <code className="text-xs">{cnameTarget}</code> before verifying.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="domain">Domain</Label>
              <Input
                id="domain"
                value={newDomain}
                onChange={(e) => setNewDomain(e.target.value)}
                placeholder="app.example.com"
                className="font-mono"
              />
            </div>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setShowAddDialog(false)}
              disabled={saving}
            >
              Cancel
            </Button>
            <Button onClick={handleAdd} disabled={saving || !newDomain.trim()}>
              {saving && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Add Domain
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <Dialog open={!!deleteTarget} onOpenChange={() => setDeleteTarget(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Remove Custom Domain</DialogTitle>
            <DialogDescription>
              Are you sure you want to remove this domain?
            </DialogDescription>
          </DialogHeader>
          {deleteTarget && (
            <div className="py-4">
              <div className="rounded-lg border p-3">
                <code className="text-sm font-medium">{deleteTarget.domain}</code>
              </div>
              <p className="mt-3 text-sm text-muted-foreground">
                Traffic to this domain will no longer be routed to your app.
              </p>
            </div>
          )}
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setDeleteTarget(null)}
              disabled={deleting}
            >
              Cancel
            </Button>
            <Button variant="destructive" onClick={handleDelete} disabled={deleting}>
              {deleting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Remove
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
