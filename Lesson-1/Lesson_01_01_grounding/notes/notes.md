# Understanding Large Language Models

Large language models are trained using next-token prediction. At massive scale, this simple objective produces surprisingly sophisticated capabilities.

## Transformer Architecture

The transformer model, introduced in 2017, revolutionized the field by enabling parallel processing of tokens through self-attention mechanisms. This allowed training on vastly larger datasets compared to earlier sequential approaches like RNNs. Contemporary models demonstrate impressive context capacity—Claude supports up to 200k tokens while GPT-4 handles 128k.

## Training and Alignment

Models undergo post-training enhancement through two main approaches: traditional RLHF, where human raters rank outputs to train reward models, and Anthropic's Constitutional AI method, which allows the model to critique and revise its own outputs based on a set of principles.

## Scaling Laws

Research on scaling laws demonstrates that model capabilities improve consistently with increased computational resources, training data, and parameters. DeepMind's research later revealed that many existing models were undertrained relative to their size, suggesting inefficient resource allocation in earlier designs.
