"""
Webhook Service Module

This module handles webhook notifications for simulation completion.
Provides functionality to register webhooks, send notifications, and manage
webhook security with HMAC signatures.
"""

import aiohttp
import asyncio
import hashlib
import hmac
import json
import logging
import time
from datetime import datetime
from typing import Optional, List, Dict

from ..models.schemas import WebhookResponse

logger = logging.getLogger(__name__)


class WebhookService:
    """Service for managing and sending webhook notifications"""

    def __init__(self):
        self.timeout = aiohttp.ClientTimeout(total=30)

    async def send_webhook_notification(
            self,
            webhook_url: str,
            payload: dict,
            secret: Optional[str] = None
    ) -> bool:
        """
        Send webhook notification when simulation completes
        
        Args:
            webhook_url: URL to send the webhook to
            payload: Data to send in the webhook
            secret: Optional secret for HMAC signature verification
            
        Returns:
            bool: True if webhook was sent successfully, False otherwise
        """
        try:
            headers = {"Content-Type": "application/json"}

            # Add signature if secret provided
            if secret:
                payload_str = json.dumps(payload, sort_keys=True)
                signature = hmac.new(
                    secret.encode('utf-8'),
                    payload_str.encode('utf-8'),
                    hashlib.sha256
                ).hexdigest()
                headers["X-Webhook-Signature"] = f"sha256={signature}"

            async with aiohttp.ClientSession() as session:
                async with session.post(
                        webhook_url,
                        json=payload,
                        headers=headers,
                        timeout=self.timeout
                ) as response:
                    if response.status == 200:
                        logger.info(f"Webhook notification sent successfully to {webhook_url}")
                        return True
                    else:
                        logger.warning(
                            f"Webhook notification failed with status {response.status} for {webhook_url}"
                        )
                        return False

        except asyncio.TimeoutError:
            logger.error(f"Webhook notification timeout for {webhook_url}")
            return False
        except Exception as e:
            logger.error(f"Error sending webhook notification to {webhook_url}: {str(e)}")
            return False

    async def trigger_webhooks(
            self,
            simulation_id: str,
            status: str,
            webhooks: List[Dict[str, str]],
            error_message: Optional[str] = None
    ) -> None:
        """
        Trigger all registered webhooks for a simulation
        
        Args:
            simulation_id: ID of the simulation
            status: Current status of the simulation
            webhooks: List of webhook configurations
            error_message: Optional error message if simulation failed
        """
        if not webhooks:
            logger.debug(f"No webhooks registered for simulation {simulation_id}")
            return

        # Create webhook payload
        payload = WebhookResponse(
            simulation_id=simulation_id,
            status=status,
            result_url=f"/simulationResult/{simulation_id}" if status == "completed" else None,
            error_message=error_message,
            timestamp=str(time.time())
        ).model_dump()

        logger.info(f"Triggering {len(webhooks)} webhooks for simulation {simulation_id}")

        # Send notifications to all registered webhooks asynchronously
        tasks = []
        for webhook in webhooks:
            task = asyncio.create_task(
                self.send_webhook_notification(
                    webhook["url"],
                    payload,
                    webhook.get("secret")
                )
            )
            tasks.append(task)

        # Wait for all webhook notifications to complete
        if tasks:
            results = await asyncio.gather(*tasks, return_exceptions=True)
            success_count = sum(1 for result in results if result is True)
            logger.info(
                f"Webhook notifications completed for simulation {simulation_id}: "
                f"{success_count}/{len(tasks)} successful"
            )

    def validate_webhook_url(self, url: str) -> bool:
        """
        Validate webhook URL format
        
        Args:
            url: URL to validate
            
        Returns:
            bool: True if URL is valid, False otherwise
        """
        try:
            from urllib.parse import urlparse
            parsed = urlparse(url)
            return all([parsed.scheme in ('http', 'https'), parsed.netloc])
        except Exception:
            return False

    def create_webhook_signature(self, payload: str, secret: str) -> str:
        """
        Create HMAC signature for webhook payload
        
        Args:
            payload: JSON payload as string
            secret: Secret key for signing
            
        Returns:
            str: HMAC signature in format 'sha256=<hash>'
        """
        signature = hmac.new(
            secret.encode('utf-8'),
            payload.encode('utf-8'),
            hashlib.sha256
        ).hexdigest()
        return f"sha256={signature}"

    def verify_webhook_signature(
            self,
            payload: str,
            signature: str,
            secret: str
    ) -> bool:
        """
        Verify webhook signature
        
        Args:
            payload: JSON payload as string
            signature: Signature to verify (format: 'sha256=<hash>')
            secret: Secret key used for signing
            
        Returns:
            bool: True if signature is valid, False otherwise
        """
        try:
            expected_signature = self.create_webhook_signature(payload, secret)
            return hmac.compare_digest(signature, expected_signature)
        except Exception:
            return False


# Global webhook service instance
webhook_service = WebhookService()


def get_webhook_service() -> WebhookService:
    """Dependency to get webhook service instance"""
    return webhook_service
