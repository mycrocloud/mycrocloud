### Install AWS Load Balancer Controller (TODO: automate this with terraform)
Step 1: Create IAM Role using eksctl
```bash
curl -O https://raw.githubusercontent.com/kubernetes-sigs/aws-load-balancer-controller/v2.13.3/docs/install/iam_policy.json
``

```bash
aws iam create-policy \
    --policy-name AWSLoadBalancerControllerIAMPolicy \
    --policy-document file://iam_policy.json 
    --region ap-northeast-1
    --profile personal
```

```bash
eksctl create iamserviceaccount \
    --cluster=mycrocloud \
    --namespace=kube-system \
    --name=aws-load-balancer-controller \
    --attach-policy-arn=arn:aws:iam::<AWS_ACCOUNT_ID>:policy/AWSLoadBalancerControllerIAMPolicy \
    --override-existing-serviceaccounts \
    --region ap-northeast-1 \
    --profile personal 
    --approve
```

Step 2: Install AWS Load Balancer Controller
```bash
helm repo add eks https://aws.github.io/eks-charts
```
```bash
helm repo update
```
```bash
helm install aws-load-balancer-controller eks/aws-load-balancer-controller \
  -n kube-system \
  --set clusterName=mycrocloud \
  --set serviceAccount.create=false \
  --set serviceAccount.name=aws-load-balancer-controller \
  --version 1.13.0
```